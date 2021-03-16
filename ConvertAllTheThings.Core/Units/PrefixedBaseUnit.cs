﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertAllTheThings.Core
{
    public class PrefixedBaseUnit : PrefixedUnit, IBaseUnit
    {
        public override BaseUnit Unit { get; }

        public PrefixedBaseUnit(BaseUnit unit, Prefix prefix)
            : base(unit, prefix)
        {
            Unit = unit;
            MaybeBaseUnitComposition = new(this);
        }
    }
}
