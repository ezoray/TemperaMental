using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Settings;
using TemperaMental.Utils;

namespace TemperaMental.Transforms
{
    public class FlipTransformService : TransformBaseService
    {
        int gridWidth;
        int gridHeight;

        protected ulong[] intermediateGroups;
        protected ulong[] tickStartSnapshot;

        protected override void Awake()
        {
            base.Awake();

            gridWidth = ConfigRegistry.Grid.GridWidth;
            gridHeight = ConfigRegistry.Grid.GridHeight;

            allowedDirections = TransformDirections.Flip;
            latchableDirections = TransformLatchableDirections.Flip;

            intermediateGroups = new ulong[ConfigRegistry.Grid.EmitterCount];
            tickStartSnapshot = new ulong[ConfigRegistry.Grid.EmitterCount];
        }

        public override ulong[] DoTransform(ulong[] groups)
        {
            if (TransformMode == TransformMode.Simple)
            {
                TransformDirections directions = GetDirections();

                if (directions == TransformDirections.None)
                    return groups;

                return DoSingleTransform(groups, directions);
            }
            else
            {
                // copy input into intermediate buffer so each emitter's result
                // feeds into the next pass without aliasing transformedGroups
                System.Array.Copy(groups, intermediateGroups, groups.Length);

                // frozen snapshot of tick-start state — each emitter's turn must read
                // its OWN bits from here, not from intermediateGroups, since an earlier
                // emitter's turn this same tick may have already reassigned bits into
                // this emitter's slot via 2-Lane entry; re-reading the live slot would
                // re-process those already-placed foreign bits a second time
                System.Array.Copy(groups, tickStartSnapshot, groups.Length);

                for (int i = 0; i < 4; i++)
                {
                    if (!ShouldEmitterFire(i))
                        continue;

                    TransformDirections direction = GetEmitterDirections(i);

                    if (direction == TransformDirections.None)
                        continue;

                    DoSingleTransformForEmitter(intermediateGroups, tickStartSnapshot[i], direction, i);
                }

                return intermediateGroups;
            }
        }

        // simple mode — flips all active emitters with the same direction
        protected override ulong[] DoSingleTransform(ulong[] groups, TransformDirections direction)
        {
            bool hasHorizontal =
                (direction & TransformDirections.Left) != 0 ||
                (direction & TransformDirections.Right) != 0;

            bool hasVertical =
                (direction & TransformDirections.Up) != 0 ||
                (direction & TransformDirections.Down) != 0;

            int groupCount = groups.Length;
            bool[] twoLaneActive = EmitterSettingsManager.CurrentTwoLanes;

            // clear/pass-through every slot up front, in one pass, before any bit is
            // written — an active emitter's slot can receive lane-reassigned bits from
            // a DIFFERENT (earlier-processed) emitter's turn, so it can no longer be
            // safely cleared mid-stream when that slot's own turn comes around
            for (int i = 0; i < groupCount; i++)
            {
                TransformActiveEmitters emitterFlag = (TransformActiveEmitters)(1 << i);

                transformedGroups[i] = (ActiveEmitters & emitterFlag) == 0
                    ? groups[i]
                    : 0;
            }

            for (int i = 0; i < groupCount; i++)
            {
                TransformActiveEmitters emitterFlag = (TransformActiveEmitters)(1 << i);

                if ((ActiveEmitters & emitterFlag) == 0)
                    continue;

                ulong mask = groups[i];

                for (int x = 0; x < gridWidth; x++)
                {
                    int flippedX = (gridWidth - 1) - x;

                    for (int y = 0; y < gridHeight; y++)
                    {
                        int index = (x * gridHeight) + y;
                        ulong bit = 1UL << index;

                        // Skip unset bits
                        if ((mask & bit) == 0)
                            continue;

                        int newX = hasHorizontal ? flippedX : x;
                        int newY = hasVertical ? (gridHeight - 1) - y : y;

                        int newIndex = (newX * gridHeight) + newY;
                        ulong newBit = 1UL << newIndex;

                        // resolve which emitter this bit actually belongs to at its destination —
                        // a 2-Lane emitter has exclusive claim on its lane regardless of active state
                        int laneOwner = EmitterUtils.GetLaneOwner(newIndex, twoLaneActive);
                        int destinationEmitterId = laneOwner != -1 ? laneOwner : i;

                        // skip if inactive emitter occupies destination
                        bool blocked = false;

                        for (int j = 0; j < groupCount; j++)
                        {
                            TransformActiveEmitters otherEmitterFlag = (TransformActiveEmitters)(1 << j);

                            // ignore active emitters
                            if ((ActiveEmitters & otherEmitterFlag) != 0)
                                continue;

                            if ((groups[j] & newBit) != 0)
                            {
                                blocked = true;
                                break;
                            }
                        }

                        if (!blocked)
                            transformedGroups[destinationEmitterId] |= newBit;
                    }
                }
            }

            return transformedGroups;
        }

        // immediate (unlatched) single-press transform for Individual mode — applies
        // to the currently selected emitter only. A single immediate call has no
        // cross-call ordering hazard (unlike the latched per-tick loop in DoTransform),
        // so the input groups array itself is a safe source for the emitter's own mask.
        protected override ulong[] DoSingleTransformForSelectedEmitter(ulong[] groups, TransformDirections direction)
        {
            System.Array.Copy(groups, intermediateGroups, groups.Length);

            int emitterId = IndividualEmitter;
            DoSingleTransformForEmitter(intermediateGroups, intermediateGroups[emitterId], direction, emitterId);

            return intermediateGroups;
        }

        // individual mode — flips only the specified emitter in place within intermediateGroups.
        // 'ownMask' is this emitter's bits as they were at the START of this tick — not
        // read live from groups[emitterId], since an earlier-firing emitter this same
        // tick may have already reassigned bits into this slot via 2-Lane entry, and
        // those must not be re-processed as if they belonged to this emitter's turn
        private void DoSingleTransformForEmitter(ulong[] groups, ulong ownMask, TransformDirections direction, int emitterId)
        {
            bool hasHorizontal =
                (direction & TransformDirections.Left) != 0 ||
                (direction & TransformDirections.Right) != 0;

            bool hasVertical =
                (direction & TransformDirections.Up) != 0 ||
                (direction & TransformDirections.Down) != 0;

            int groupCount = groups.Length;
            bool[] twoLaneActive = EmitterSettingsManager.CurrentTwoLanes;

            ulong mask = ownMask;

            // only remove THIS emitter's own original bits from its slot — anything
            // else currently in the slot (reassigned in by an earlier emitter's turn
            // this same tick) must survive
            groups[emitterId] &= ~mask;

            for (int x = 0; x < gridWidth; x++)
            {
                int flippedX = (gridWidth - 1) - x;

                for (int y = 0; y < gridHeight; y++)
                {
                    int index = (x * gridHeight) + y;
                    ulong bit = 1UL << index;

                    // skip unset bits
                    if ((mask & bit) == 0)
                        continue;

                    int newX = hasHorizontal ? flippedX : x;
                    int newY = hasVertical ? (gridHeight - 1) - y : y;

                    int newIndex = (newX * gridHeight) + newY;
                    ulong newBit = 1UL << newIndex;

                    // resolve which emitter this bit actually belongs to at its destination
                    int laneOwner = EmitterUtils.GetLaneOwner(newIndex, twoLaneActive);
                    int destinationEmitterId = laneOwner != -1 ? laneOwner : emitterId;

                    // skip if any other emitter already occupies destination — excludes
                    // only the source emitter (this bit can't block its own prior spot);
                    // the destination's existing content (including the lane owner's own
                    // bits, when destination differs from source) is a real collision
                    bool blocked = false;

                    for (int j = 0; j < groupCount; j++)
                    {
                        if (j == emitterId)
                            continue;

                        if ((groups[j] & newBit) != 0)
                        {
                            blocked = true;
                            break;
                        }
                    }

                    if (!blocked)
                        groups[destinationEmitterId] |= newBit;
                }
            }
        }
    }
}