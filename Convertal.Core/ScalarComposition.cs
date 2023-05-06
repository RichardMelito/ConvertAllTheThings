
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Convertal.Core.Extensions;

namespace Convertal.Core;

public class ScalarComposition<T> : NamedComposition<T>, 
    IScalar<ScalarComposition<T>, VectorComposition<T>>
    where T : IMaybeNamed, IVectorOrScalar
{
    public static readonly ScalarComposition<T> Empty;

    public VectorComposition<T>? VectorAnalog
    {
        get
        {
            if (this == Empty)
                return VectorComposition<T>.Empty;

            var emptyKvp = new KeyValuePair<T, decimal>((T)T.GetEmpty(true), 1m);
            return new(
                this
                .Append(emptyKvp)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }
    }

    public override bool IsVector => false;

    static ScalarComposition()
    {
        Empty = new ScalarComposition<T>(new Dictionary<T, decimal>().ToImmutableDictionary());
    }

    internal ScalarComposition(IReadOnlyDictionary<T, decimal> composition)
        : base(MakeAllInDictScalar(composition))
    {
    }

    public ScalarComposition(T key)
        : base(key.IsVector ? key : throw new InvalidOperationException())
    {
    }

    public ScalarComposition<T> Pow(decimal power)
    {
        if (power == 0)
            return Empty;

        if (power == 1)
            return this;

        SortedDictionary<T, decimal> newDict = new();
        foreach (var (key, currentPower) in this)
            newDict.Add(key, currentPower * power);

        return new ScalarComposition<T>(newDict.AsReadOnly());
    }


    private static ScalarComposition<T> MultiplyOrDivide(
        ScalarComposition<T> lhs,
        ScalarComposition<T> rhs,
        bool multiplication)
    {
        var multiplyFactor = multiplication ? 1.0m : -1.0m;
        SortedDictionary<T, decimal> resultingComposition = new();

        var keysInBothSides = lhs.Keys.Intersect(rhs.Keys);
        foreach (var bothSidesKey in keysInBothSides)
        {
            var resultingPower = lhs[bothSidesKey] +
                (multiplyFactor * rhs[bothSidesKey]);

            if (resultingPower != 0.0m)
                resultingComposition[bothSidesKey] = resultingPower;
        }

        var keysInLhs = lhs.Keys.Except(keysInBothSides);
        foreach (var lhsKey in keysInLhs)
            resultingComposition[lhsKey] = lhs[lhsKey];

        var keysInRhs = rhs.Keys.Except(keysInBothSides);
        foreach (var rhsKey in keysInRhs)
            resultingComposition[rhsKey] = rhs[rhsKey] * multiplyFactor;

        if (resultingComposition.Count == 0)
            return Empty;

        return new ScalarComposition<T>(resultingComposition.AsReadOnly());
    }

    public ScalarComposition<T> Multiply(ScalarComposition<T> other) => this * other;
    public VectorComposition<T> Multiply(VectorComposition<T> vector) => this * vector;
    public ScalarComposition<T> Divide(ScalarComposition<T> other) => this / other;

    public static ScalarComposition<T> operator *(ScalarComposition<T> lhs, ScalarComposition<T> rhs)
    {
        return new(MultiplyOrDivide(lhs, rhs, true));
    }

    public static ScalarComposition<T> operator /(ScalarComposition<T> lhs, ScalarComposition<T> rhs)
    {
        return new(MultiplyOrDivide(lhs, rhs, false));
    }

    public static VectorComposition<T> operator *(VectorComposition<T> vector, ScalarComposition<T> scalar) => scalar * vector;

    public static VectorComposition<T> operator *(ScalarComposition<T> scalar, VectorComposition<T> vector)
    {
        SortedDictionary<T, decimal> resultingComposition = new();

        var keysInBothSides = scalar.Keys.Intersect(vector.Keys);
        foreach (var bothSidesKey in keysInBothSides)
        {
            var resultingPower = scalar[bothSidesKey] + vector[bothSidesKey];

            if (resultingPower != 0.0m)
                resultingComposition[bothSidesKey] = resultingPower;
        }

        var keysInLhs = scalar.Keys.Except(keysInBothSides);
        foreach (var lhsKey in keysInLhs)
            resultingComposition[lhsKey] = scalar[lhsKey];

        var keysInRhs = vector.Keys.Except(keysInBothSides);
        foreach (var rhsKey in keysInRhs)
            resultingComposition[rhsKey] = vector[rhsKey];

        if (resultingComposition.Count == 0)
            return VectorComposition<T>.Empty;

        return new VectorComposition<T>(resultingComposition.AsReadOnly());
    }
}
