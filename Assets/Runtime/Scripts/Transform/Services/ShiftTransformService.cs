using System;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Settings;
using TemperaMental.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Transforms
{
    public class ShiftTransformService : TransformBaseService
    {
        int gridWidth;
        int gridHeight;

        bool isWrapping;

        protected ulong[] tickStartSnapshot;

        [SerializeField] UnityEvent<bool> onWrapStateChanged;

        protected override void Awake()
        {
            base.Awake();

            gridWidth = ConfigRegistry.Grid.GridWidth;
            gridHeight = ConfigRegistry.Grid.GridHeight;

            allowedDirections = TransformDirections.Shift;
            latchableDirections = TransformLatchableDirections.Shift;

            tickStartSnapshot = new ulong[ConfigRegistry.Grid.EmitterCount];
        }

        public override void ResetTransform()
        {
            base.ResetTransform();
            isWrapping = false;
        }

        public void ToggleWrap()
        {
            isWrapping = !isWrapping;

            onWrapStateChanged?.Invoke(isWrapping);
        }

        public override ulong[] DoTransform(ulong[] groups)
        {
            if (TransformMode == TransformMode.Simple)
            {
                TransformDirections directions = GetDirections();

                if (directions == TransformDirections.None)
                    return groups;

                ulong[] result = DoSingleTransform(groups, directions);
                EmitterUtils.ReassignLaneBitsAcrossActive(result, EmitterSettingsManager.CurrentTwoLanes);
                return result;
            }
            else
            {
                Array.Copy(groups, tickStartSnapshot, groups.Length);
                Array.Copy(groups, transformedGroups, groups.Length);

                bool[] twoLaneActive = EmitterSettingsManager.CurrentTwoLanes;

                // all active 2-Lane territory — positions inside active lanes are always
                // claimable via reassignment and must never count as "blocked" for an
                // incoming shifted bit, regardless of what currently occupies them
                ulong allLaneMask = 0;
                for (int i = 0; i < transformedGroups.Length; i++)
                {
                    if (twoLaneActive[i])
                        allLaneMask |= EmitterUtils.LaneMasks[i];
                }

                // Phase 1 — clear each firing emitter's own original bits from the
                // result buffer before any shifting begins. This cleanly separates
                // "bits about to move" from "bits already received via lane reassignment
                // from an earlier emitter this same tick," which share the same slot
                // and cannot be distinguished by bitmask alone once mixed together
                for (int i = 0; i < 4; i++)
                {
                    if (!ShouldEmitterFire(i)) continue;
                    if (GetEmitterDirections(i) == TransformDirections.None) continue;
                    transformedGroups[i] &= ~tickStartSnapshot[i];
                }

                // Phase 2 — shift each firing emitter's original bits and write to
                // their new positions, including lane reassignment
                for (int i = 0; i < 4; i++)
                {
                    if (!ShouldEmitterFire(i)) continue;

                    TransformDirections direction = GetEmitterDirections(i);
                    if (direction == TransformDirections.None) continue;

                    ulong ownMask = tickStartSnapshot[i];
                    if (ownMask == 0) continue;

                    // build othersOccupied from current live result, excluding all active
                    // 2-Lane territory — those positions are claimable via reassignment
                    // and must not block incoming bits even if currently occupied
                    ulong othersOccupied = 0;
                    for (int j = 0; j < transformedGroups.Length; j++)
                    {
                        if (j != i)
                            othersOccupied |= (transformedGroups[j] & ~allLaneMask);
                    }

                    bool hasHorizontal =
                        (direction & TransformDirections.Left) != 0 ||
                        (direction & TransformDirections.Right) != 0;

                    bool hasVertical =
                        (direction & TransformDirections.Up) != 0 ||
                        (direction & TransformDirections.Down) != 0;

                    TransformDirections horizontalDirection =
                        direction & (TransformDirections.Left | TransformDirections.Right);

                    TransformDirections verticalDirection =
                        direction & (TransformDirections.Up | TransformDirections.Down);

                    ulong mask = ownMask;

                    if (hasHorizontal)
                        mask = ShiftBitmask(mask, horizontalDirection, isWrapping);

                    if (hasVertical)
                        mask = ShiftBitmask(mask, verticalDirection, isWrapping);

                    mask &= ~othersOccupied;

                    EmitterUtils.ReassignLaneBits(transformedGroups, mask, i, twoLaneActive);
                }

                return transformedGroups;
            }
        }

        // immediate (unlatched) single-press transform for Individual mode
        protected override ulong[] DoSingleTransformForSelectedEmitter(ulong[] groups, TransformDirections direction)
        {
            int emitterId = IndividualEmitter;
            return DoSingleTransformForEmitter(groups, groups[emitterId], direction, emitterId);
        }

        // simple mode — shifts all active emitters with the same direction
        protected override ulong[] DoSingleTransform(ulong[] groups, TransformDirections direction)
        {
            bool hasHorizontal =
                (direction & TransformDirections.Left) != 0 ||
                (direction & TransformDirections.Right) != 0;

            bool hasVertical =
                (direction & TransformDirections.Up) != 0 ||
                (direction & TransformDirections.Down) != 0;

            TransformDirections horizontalDirection =
                direction & (TransformDirections.Left | TransformDirections.Right);

            TransformDirections verticalDirection =
                direction & (TransformDirections.Up | TransformDirections.Down);

            int groupCount = groups.Length;

            ulong inactiveOccupied = 0;

            // build occupied mask from inactive emitters
            for (int i = 0; i < groupCount; i++)
            {
                TransformActiveEmitters emitterFlag = (TransformActiveEmitters)(1 << i);

                if ((ActiveEmitters & emitterFlag) == 0)
                    inactiveOccupied |= groups[i];
            }

            // transform active emitters
            for (int i = 0; i < groupCount; i++)
            {
                TransformActiveEmitters emitterFlag = (TransformActiveEmitters)(1 << i);

                // pass inactive emitters through untouched
                if ((ActiveEmitters & emitterFlag) == 0)
                {
                    transformedGroups[i] = groups[i];
                    continue;
                }

                ulong mask = groups[i];

                if (hasHorizontal)
                    mask = ShiftBitmask(mask, horizontalDirection, isWrapping);

                if (hasVertical)
                    mask = ShiftBitmask(mask, verticalDirection, isWrapping);

                // prevent overlap with inactive emitters
                transformedGroups[i] = mask & ~inactiveOccupied;
            }

            // last-writer-wins collision resolution between active emitters
            for (int i = 0; i < transformedGroups.Length; i++)
            {
                TransformActiveEmitters emitterFlagI = (TransformActiveEmitters)(1 << i);

                if ((ActiveEmitters & emitterFlagI) == 0)
                    continue;

                ulong movedInto = transformedGroups[i] & ~groups[i];

                for (int j = 0; j < transformedGroups.Length; j++)
                {
                    if (i == j)
                        continue;

                    TransformActiveEmitters emitterFlagJ = (TransformActiveEmitters)(1 << j);

                    if ((ActiveEmitters & emitterFlagJ) == 0)
                        continue;

                    transformedGroups[j] &= ~movedInto;
                }
            }

            return transformedGroups;
        }

        // individual mode — shifts only the specified emitter. 'ownMask' is this emitter's
        // bits as they were at the START of this tick — not read live from groups[emitterId],
        // since an earlier-firing emitter this same tick may have already reassigned bits
        // into this slot via 2-Lane entry, and those must not be re-shifted a second time
        private ulong[] DoSingleTransformForEmitter(ulong[] groups, ulong ownMask, TransformDirections direction, int emitterId)
        {
            bool hasHorizontal =
                (direction & TransformDirections.Left) != 0 ||
                (direction & TransformDirections.Right) != 0;

            bool hasVertical =
                (direction & TransformDirections.Up) != 0 ||
                (direction & TransformDirections.Down) != 0;

            TransformDirections horizontalDirection =
                direction & (TransformDirections.Left | TransformDirections.Right);

            TransformDirections verticalDirection =
                direction & (TransformDirections.Up | TransformDirections.Down);

            int groupCount = groups.Length;

            ulong othersOccupied = 0;

            for (int i = 0; i < groupCount; i++)
            {
                if (i != emitterId)
                    othersOccupied |= groups[i];
            }

            Array.Copy(groups, transformedGroups, groupCount);

            // only remove THIS emitter's own original bits from its slot — anything
            // else currently in the slot (reassigned in by an earlier emitter's turn
            // this same tick) must survive
            transformedGroups[emitterId] &= ~ownMask;

            ulong mask = ownMask;

            if (hasHorizontal)
                mask = ShiftBitmask(mask, horizontalDirection, isWrapping);

            if (hasVertical)
                mask = ShiftBitmask(mask, verticalDirection, isWrapping);

            mask &= ~othersOccupied;

            // reassign any bits that now fall inside a different emitter's 2-Lane territory
            EmitterUtils.ReassignLaneBits(transformedGroups, mask, emitterId, EmitterSettingsManager.CurrentTwoLanes);

            return transformedGroups;
        }

        private ulong ShiftBitmask(ulong mask, TransformDirections direction, bool wrap)
        {
            return direction switch
            {
                TransformDirections.Left => ShiftLeft(mask, wrap),
                TransformDirections.Right => ShiftRight(mask, wrap),
                TransformDirections.Up => ShiftUp(mask, wrap),
                TransformDirections.Down => ShiftDown(mask, wrap),
                _ => throw new ArgumentOutOfRangeException(nameof(direction))
            };
        }

        // move emitters to lower x (subtract one column)
        private ulong ShiftLeft(ulong mask, bool wrap)
        {
            ulong lost = mask & ColumnMask(0);
            ulong shifted = (mask >> gridHeight) & ValidMask();

            if (wrap)
                shifted |= lost << ((gridWidth - 1) * gridHeight);

            return shifted;
        }

        // move emitters to higher x (add one column)
        private ulong ShiftRight(ulong mask, bool wrap)
        {
            ulong lost = mask & ColumnMask(gridWidth - 1);
            ulong shifted = (mask << gridHeight) & ValidMask();

            if (wrap)
                shifted |= lost >> ((gridWidth - 1) * gridHeight);

            return shifted;
        }

        // move emitters to higher y (decrease index within each column)
        private ulong ShiftUp(ulong mask, bool wrap)
        {
            ulong result = 0;

            for (int x = 0; x < gridWidth; x++)
            {
                int shift = x * gridHeight;
                ulong col = (mask & ColumnMask(x)) >> shift;
                ulong lost = col & 1UL;
                ulong shifted = col >> 1;

                if (wrap)
                    shifted |= lost << (gridHeight - 1);

                result |= shifted << shift;
            }

            return result;
        }

        // move emitters to lower y (increase index within each column)
        private ulong ShiftDown(ulong mask, bool wrap)
        {
            ulong result = 0;
            ulong colBits = (1UL << gridHeight) - 1;

            for (int x = 0; x < gridWidth; x++)
            {
                int shift = x * gridHeight;
                ulong col = (mask & ColumnMask(x)) >> shift;
                ulong lost = (col >> (gridHeight - 1)) & 1UL;
                ulong shifted = (col << 1) & colBits;

                if (wrap)
                    shifted |= lost;

                result |= shifted << shift;
            }

            return result;
        }

        private ulong ValidMask()
        {
            int totalBits = gridWidth * gridHeight;

            return totalBits >= 64
                ? ulong.MaxValue
                : (1UL << totalBits) - 1;
        }

        private ulong ColumnMask(int column)
        {
            ulong colMask = (1UL << gridHeight) - 1;
            return colMask << (column * gridHeight);
        }

        public bool Wrap
        {
            get => isWrapping;
            set => isWrapping = value;
        }
    }
}