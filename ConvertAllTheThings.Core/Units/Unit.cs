﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DecimalMath;
using ConvertAllTheThings.Core.Extensions;

namespace ConvertAllTheThings.Core
{
    public abstract class Unit : MaybeNamed, IUnit
    {
        private bool _disposed = false;

        public static EmptyUnit Empty => EmptyUnit.Empty;

        public Quantity Quantity { get; }

        public decimal FundamentalMultiplier { get; }
        public BaseComposition<IBaseUnit>? MaybeBaseUnitComposition { get; protected set; } = null;

        static Unit()
        {
            AddTypeToDictionary<Unit>();
        }

        internal static void InitializeClass() { }

        protected Unit(string? name, Quantity quantity, decimal fundamentalMultiplier)
            : base(name)
        {
            Quantity = quantity;
            FundamentalMultiplier = fundamentalMultiplier;
        }

        protected Unit(string? name, IUnit otherUnit, decimal multiplier)
            : base(name)
        {
            Quantity = otherUnit.Quantity;
            FundamentalMultiplier = otherUnit.FundamentalMultiplier * multiplier;
        }

        protected Unit(string name, BaseComposition<IBaseUnit> composition)
            : base(name)
        {
            MaybeBaseUnitComposition = composition;
            Quantity = Quantity.GetFromBaseComposition(MaybeBaseUnitComposition);
            FundamentalMultiplier = 1m;
            foreach (var (unit, power) in MaybeBaseUnitComposition.Composition)
            {
                var multiplier = DecimalEx.Pow(unit.FundamentalMultiplier, power);
                FundamentalMultiplier *= multiplier;
            }
        }

        public override IOrderedEnumerable<IMaybeNamed> GetAllDependents(ref IEnumerable<IMaybeNamed> toIgnore)
        {
            var res = IUnit.GetAllDependents(this, ref toIgnore).AsEnumerable();

            var prefixedUnitsWithThis =
                from prefixedUnit in PrefixedUnit.PrefixedUnits
                where prefixedUnit.Unit == this
                select prefixedUnit;

            res = res.Union(prefixedUnitsWithThis);
            foreach (var prefixedUnit in prefixedUnitsWithThis.Except(toIgnore))
                res = res.Union(prefixedUnit.GetAllDependents(ref toIgnore));

            res.ThrowIfSetContains(this);
            return res.SortByTypeAndName();
        }

        public override string ToString()
        {
            return MaybeName ?? MaybeBaseUnitComposition!.ToString();
        }

        public bool Equals(IUnit? other)
        {
            return base.Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), Quantity, FundamentalMultiplier);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (((IUnit)this).IsFundamental && !Quantity.Disposed)
                throw new InvalidOperationException($"Cannot dispose of" +
                    $" fundamental unit {this} without first disposing of " +
                    $"quantity {Quantity}.");

            _disposed = true;
            base.Dispose(disposing);
        }
    }
}
