using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        ReadOnlySpan<uint> x = a.GetDigits();
        ReadOnlySpan<uint> y = b.GetDigits();
        uint[] result = new uint[x.Length + y.Length];

        for (int i = 0; i < x.Length; i++) {
            ulong carry = 0;
            for (int j = 0; j < y.Length; j++) {
                ulong current = (ulong)x[i] * y[j] + result[i + j] + carry;
                result[i + j] = (uint)current;
                carry = current >> 32;
            }
            result[i + y.Length] += (uint)carry;
        }

        return new BetterBigInteger(result, false);
    }
}
