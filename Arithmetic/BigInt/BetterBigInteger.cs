using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;

namespace Arithmetic.BigInt;

public sealed class BetterBigInteger : IBigInteger
{
    // Пороги переключения между алгоритмами
    private const int KaratsubaThreshold = 32;
    private const int FftThreshold = 512;

    private static readonly IMultiplier _simpleMultiplier = new SimpleMultiplier();
    private static readonly IMultiplier _karatsubaMultiplier = new KaratsubaMultiplier();
    private static readonly IMultiplier _fftMultiplier = new FftMultiplier();

    private int _signBit;
    private uint _smallValue; // Если число маленькое, храним его прямо в этом поле, а _data == null.
    private uint[]? _data;

    public bool IsNegative => _signBit == 1;

    /// От массива цифр (little endian)
    public BetterBigInteger(uint[] digits, bool isNegative = false)
    {
        int length = digits.Length;
        while(length > 0 && digits[length - 1] == 0) {
            length--;
        }

        if(length == 0) {
            _signBit = 0;
            _smallValue = 0;
            _data = null;
            return;
        }
        else if(length == 1) {
            _signBit = isNegative ? 1 : 0;
            _smallValue = digits[0];
            _data = null;
            return;
        }

        _signBit = isNegative ? 1 : 0;

        _data = new uint[length];
        Array.Copy(digits, _data, length);
    }

    public BetterBigInteger(IEnumerable<uint> digits, bool isNegative = false)
        : this(digits.ToArray(), isNegative)
    {
    }

    private static uint ParseChar(char c, int radix)
    {
        uint val;
        if (c >= '0' && c <= '9') {
            val = (uint)(c - '0');
        }
        else if (c >= 'a' && c <= 'z') {
            val = (uint)(c - 'a' + 10);
        }
        else if (c >= 'A' && c <= 'Z') {
            val = (uint)(c - 'A' + 10);
        }
        else {
            throw new FormatException($"Invalid character: {c}");
        }

        if (val >= (uint)radix) {
            throw new FormatException($"Character {c} is out of range for radix {radix}");
        }
        return val;
    }

    public BetterBigInteger(string value, int radix)
    {
        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }
        if (radix < 2 || radix > 36) {
            throw new ArgumentOutOfRangeException(nameof(radix), "Radix must be between 2 and 36");
        }
        string trimmed = value.Trim();
        if (trimmed.Length == 0) {
            throw new FormatException("String is empty or whitespace");
        }

        int startIndex = 0;
        bool isNegative = false;
        if (trimmed[0] == '-') {
            isNegative = true;
            startIndex = 1;
        }
        else if (trimmed[0] == '+') {
            startIndex = 1;
        }

        if (startIndex == trimmed.Length) {
            throw new FormatException("String contains only a sign");
        }

        BetterBigInteger current = new BetterBigInteger([0], false);
        BetterBigInteger radixBig = new BetterBigInteger([(uint)radix], false);

        for (int i = startIndex; i < trimmed.Length; i++) {
            uint digitValue = ParseChar(trimmed[i], radix);
            current = (current * radixBig) + new BetterBigInteger([digitValue], false);
        }

        if (current.GetDigits().Length == 1 && current.GetDigits()[0] == 0) {
            _signBit = 0;
            _smallValue = 0;
            _data = null;
        }
        else {
            _signBit = isNegative ? 1 : 0;
            ReadOnlySpan<uint> resDigits = current.GetDigits();
            if (resDigits.Length == 1) {
                _smallValue = resDigits[0];
                _data = null;
            }
            else {
                _data = resDigits.ToArray();
                _smallValue = 0;
            }
        }
    }

    public ReadOnlySpan<uint> GetDigits()
    {
        return _data ?? [_smallValue];
    }

    public int CompareTo(IBigInteger? other)
    {
        if(other == null) {
            return 1;
        }
        else if(!this.IsNegative && other.IsNegative) {
            return 1;
        }
        else if(this.IsNegative && !other.IsNegative) {
            return -1;
        }

        int magnitudeComparison = CompareMagnitudes(this, other);

        if (this.IsNegative) {
            return -magnitudeComparison;
        }

        return magnitudeComparison;
    }

    private static int CompareMagnitudes(IBigInteger a, IBigInteger b)
    {
        ReadOnlySpan<uint> aDigits = a.GetDigits();
        ReadOnlySpan<uint> bDigits = b.GetDigits();

        if (aDigits.Length < bDigits.Length) {
            return -1;
        } else if (aDigits.Length > bDigits.Length) {
            return 1;
        }

        for (int i = aDigits.Length - 1; i >= 0; i--) {
            if (aDigits[i] < bDigits[i]) {
                return -1;
            }
            else if (aDigits[i] > bDigits[i]) {
                return 1;
            }
        }

        return 0;
    }

    public bool Equals(IBigInteger? other)
    {
        if (other is null) {
            return false;
        }
        else if (ReferenceEquals(this, other)) {
            return true;
        }

        return CompareTo(other) == 0;
    }

    public override bool Equals(object? obj) => obj is IBigInteger other && Equals(other);

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(_signBit);

        ReadOnlySpan<uint> digits = GetDigits();

        foreach (uint digit in digits) {
            hash.Add(digit);
        }

        return hash.ToHashCode();
    }

    public static BetterBigInteger operator +(BetterBigInteger a, BetterBigInteger b)
    {
        if (a.IsNegative == b.IsNegative) {
            uint[] sum = AddMagnitudes(a.GetDigits(), b.GetDigits());
            return new BetterBigInteger(sum, a.IsNegative);
        }
        else {
            int compare = CompareMagnitudes(a, b);
            if (compare == 0) {
                return new BetterBigInteger([0]);
            }
            if (compare > 0) {
                uint[] diff = SubtractMagnitudes(a.GetDigits(), b.GetDigits());
                return new BetterBigInteger(diff, a.IsNegative);
            }
            else {
                uint[] diff = SubtractMagnitudes(b.GetDigits(), a.GetDigits());
                return new BetterBigInteger(diff, b.IsNegative);
            }
        }
    }

    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b)
    {
        return a + (-b);
    }

    public static BetterBigInteger operator -(BetterBigInteger a)
    {
        if (a._data == null && a._smallValue == 0) {
            return a;
        }

        return new BetterBigInteger(a.GetDigits().ToArray(), !a.IsNegative);
    }

    private static (BetterBigInteger Quotient, BetterBigInteger Remainder) DivideByUint(BetterBigInteger a, uint b)
    {
        if (b == 0) {
            throw new DivideByZeroException();
        }

        ReadOnlySpan<uint> aDigits = a.GetDigits();
        uint[] quotient = new uint[aDigits.Length];
        ulong remainder = 0;

        for (int i = aDigits.Length - 1; i >= 0; i--) {
            ulong current = aDigits[i] + (remainder << 32);
            quotient[i] = (uint)(current / b);
            remainder = current % b;
        }

        return (new BetterBigInteger(quotient, a.IsNegative), new BetterBigInteger([(uint)remainder], a.IsNegative));
    }

    private static uint[] ShiftLeftMagnitude(ReadOnlySpan<uint> source, int bitShift)
    {
        if (bitShift == 0) {
            return source.ToArray();
        }

        uint[] result = new uint[source.Length + 1];
        ulong carry = 0;
        for (int i = 0; i < source.Length; i++) {
            ulong current = ((ulong)source[i] << bitShift) | carry;
            result[i] = (uint)current;
            carry = current >> 32;
        }
        result[source.Length] = (uint)carry;
        return result;
    }

    private static uint[] ShiftRightMagnitude(ReadOnlySpan<uint> source, int bitShift)
    {
        if (bitShift == 0) {
            return source.ToArray();
        }
        uint[] result = new uint[source.Length];
        uint carry = 0;
        for (int i = source.Length - 1; i >= 0; i--) {
            ulong current = source[i] | ((ulong)carry << 32);
            result[i] = (uint)(current >> bitShift);
            carry = source[i] & ((1u << bitShift) - 1);
        }
        return result;
    }

    private static (BetterBigInteger Quotient, BetterBigInteger Remainder) DivideWithRemainder(BetterBigInteger u, BetterBigInteger v)
    {
        ReadOnlySpan<uint> vDigits = v.GetDigits();
        if (vDigits.Length < 2) {
            return DivideByUint(u, vDigits[0]);
        }
        ReadOnlySpan<uint> uDigits = u.GetDigits();
        int n = vDigits.Length;
        int m = uDigits.Length - n;

        int shift = BitOperations.LeadingZeroCount(vDigits[n - 1]);
        uint[] vn = ShiftLeftMagnitude(vDigits, shift);
        uint[] un = ShiftLeftMagnitude(uDigits, shift);

        if (un.Length == uDigits.Length) {
            Array.Resize(ref un, un.Length + 1);
            un[un.Length - 1] = 0;
        }

        uint[] q = new uint[m + 1];

        for (int j = m; j >= 0; j--) {
            ulong highU = ((ulong)un[j + n] << 32) | un[j + n - 1];
            ulong qHat = highU / vn[n - 1];
            ulong rHat = highU % vn[n - 1];

            while (qHat >= 0x100000000L || (qHat * vn[n - 2] > (0x100000000L * rHat + un[j + n - 2]))) {
                qHat--;
                rHat += vn[n - 1];
                if (rHat >= 0x100000000L) {
                    break;
                }
            }

            long borrow = 0;
            for (int i = 0; i < n; i++) {
                ulong product = qHat * vn[i];
                long diff = (long)un[j + i] - (uint)product - borrow;
                un[j + i] = (uint)diff;
                borrow = (long)(product >> 32) - (diff >> 32);
            }
            long lastDiff = (long)un[j + n] - borrow;
            un[j + n] = (uint)lastDiff;

            if (lastDiff < 0) {
                qHat--;
                long carry = 0;
                for (int i = 0; i < n; i++) {
                    ulong sum = (ulong)un[j + i] + vn[i] + (ulong)carry;
                    un[j + i] = (uint)sum;
                    carry = (long)(sum >> 32);
                }
                un[j + n] = (uint)((ulong)un[j + n] + (ulong)carry);
            }

            q[j] = (uint)qHat;
        }

        uint[] remainder = ShiftRightMagnitude(un.AsSpan(0, n), shift);

        return (new BetterBigInteger(q), new BetterBigInteger(remainder));
    }

    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b)
    {
        if (b.GetDigits().Length == 1 && b.GetDigits()[0] == 0) {
            throw new DivideByZeroException();
        }

        if (CompareMagnitudes(a, b) < 0) {
            return new BetterBigInteger([0]);
        }

        var (quotient, _) = DivideWithRemainder(a, b);

        bool resultNegative = a.IsNegative ^ b.IsNegative;

        return new BetterBigInteger(quotient.GetDigits().ToArray(), resultNegative);
    }

    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b)
    {
        if (b.GetDigits().Length == 1 && b.GetDigits()[0] == 0) {
            throw new DivideByZeroException();
        }

        if (CompareMagnitudes(a, b) < 0) {
            return a;
        }

        var (_, remainder) = DivideWithRemainder(a, b);

        return new BetterBigInteger(remainder.GetDigits().ToArray(), a.IsNegative);
    }

    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
    {
        if (IsZero(a) || IsZero(b)) {
            return new BetterBigInteger([0]);
        }

        bool resultIsNegative = a.IsNegative ^ b.IsNegative;

        int maxLen = Math.Max(a.GetDigits().Length, b.GetDigits().Length);

        IMultiplier strategy = maxLen switch {
            < KaratsubaThreshold => _simpleMultiplier,
            < FftThreshold => _karatsubaMultiplier,
            _ => _fftMultiplier
        };

        BetterBigInteger absA = a.IsNegative ? -a : a;
        BetterBigInteger absB = b.IsNegative ? -b : b;

        BetterBigInteger result = strategy.Multiply(absA, absB);

        return resultIsNegative ? -result : result;
    }


    public static BetterBigInteger operator ~(BetterBigInteger a)
    {
        return -(a + new BetterBigInteger([1]));
    }

    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b) =>
        BitwiseOpImpl(a, b, (x, y) => x & y, a.IsNegative & b.IsNegative);

    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b) =>
        BitwiseOpImpl(a, b, (x, y) => x | y, a.IsNegative | b.IsNegative);

    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b) =>
        BitwiseOpImpl(a, b, (x, y) => x ^ y, a.IsNegative ^ b.IsNegative);

    private static BetterBigInteger BitwiseOpImpl(BetterBigInteger a, BetterBigInteger b, Func<uint, uint, uint> op, bool resultNegative)
    {
        ReadOnlySpan<uint> aDigits = a.GetDigits();
        ReadOnlySpan<uint> bDigits = b.GetDigits();

        int maxLen = Math.Max(aDigits.Length, bDigits.Length) + 1;
        uint[] resWords = new uint[maxLen];

        ulong aCarry = a.IsNegative ? 1ul : 0ul;
        ulong bCarry = b.IsNegative ? 1ul : 0ul;

        for (int i = 0; i < maxLen; i++) {
            uint aWord = i < aDigits.Length ? aDigits[i] : 0u;
            if (a.IsNegative) {
                ulong sum = (~aWord) + aCarry;
                aWord = (uint)sum;
                aCarry = sum >> 32;
            }

            uint bWord = i < bDigits.Length ? bDigits[i] : 0u;
            if (b.IsNegative) {
                ulong sum = (~bWord) + bCarry;
                bWord = (uint)sum;
                bCarry = sum >> 32;
            }

            resWords[i] = op(aWord, bWord);
        }

        if (resultNegative) {
            ulong resCarry = 1ul;
            for (int i = 0; i < maxLen; i++) {
                ulong sum = (~resWords[i]) + resCarry;
                resWords[i] = (uint)sum;
                resCarry = sum >> 32;
            }
        }

        return new BetterBigInteger(resWords, resultNegative);
    }

    public static BetterBigInteger operator <<(BetterBigInteger a, int shift)
    {
        if (shift < 0) {
            return a >> -shift;
        }
        if (shift == 0) {
            return a;
        }
        int wordShift = shift / 32;
        int bitShift = shift % 32;

        uint[] shiftedMagnitude = ShiftLeftMagnitude(a.GetDigits(), bitShift);

        if (wordShift > 0) {
            uint[] finalResult = new uint[shiftedMagnitude.Length + wordShift];
            Array.Copy(shiftedMagnitude, 0, finalResult, wordShift, shiftedMagnitude.Length);
            return new BetterBigInteger(finalResult, a.IsNegative);
        }

        return new BetterBigInteger(shiftedMagnitude, a.IsNegative);
    }

    public static BetterBigInteger operator >> (BetterBigInteger a, int shift)
    {
        if (shift < 0) {
            return a << -shift;
        }
        if (shift == 0) {
            return a;
        }

        ReadOnlySpan<uint> aDigits = a.GetDigits();

        int wordShift = shift / 32;
        int bitShift = shift % 32;

        bool hasDroppedBits = false;

        for (int i = 0; i < Math.Min(wordShift, aDigits.Length); i++) {
            if (aDigits[i] != 0) {
                hasDroppedBits = true;
                break;
            }
        }

        if (!hasDroppedBits && wordShift < aDigits.Length && bitShift > 0) {
            uint droppedMask = (1u << bitShift) - 1;
            if ((aDigits[wordShift] & droppedMask) != 0) {
                hasDroppedBits = true;
            }
        }

        if (wordShift >= aDigits.Length) {
            return a.IsNegative ? new BetterBigInteger([1], true) : new BetterBigInteger([0], false);
        }

        ReadOnlySpan<uint> relevantDigits = aDigits.Slice(wordShift);
        uint[] shiftedMagnitude = ShiftRightMagnitude(relevantDigits, bitShift);

        BetterBigInteger result = new BetterBigInteger(shiftedMagnitude, a.IsNegative);
        if (a.IsNegative && hasDroppedBits) {
            result -= new BetterBigInteger([1]);
        }

        return result;
    }

    public static bool operator ==(BetterBigInteger a, BetterBigInteger b) => Equals(a, b);
    public static bool operator !=(BetterBigInteger a, BetterBigInteger b) => !Equals(a, b);
    public static bool operator <(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) < 0;
    public static bool operator >(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) > 0;
    public static bool operator <=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) >= 0;

    public override string ToString() => ToString(10);

    public string ToString(int radix)
    {
        if (radix < 2 || radix > 36) {
            throw new ArgumentOutOfRangeException(nameof(radix), "Radix must be between 2 and 36");
        }

        if (IsZero(this)) {
            return "0";
        }

        BetterBigInteger current = new BetterBigInteger(this.GetDigits().ToArray(), false);
        List<char> digitsList = new List<char>();
        const string CharSet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        while (!IsZero(current)) {
            var (quot, rem) = DivideByUint(current, (uint)radix);
            uint remainderValue = rem.GetDigits()[0];
            digitsList.Add(CharSet[(int)remainderValue]);
            current = quot;
        }

        if (this.IsNegative) {
            digitsList.Add('-');
        }

        digitsList.Reverse();
        return new string(digitsList.ToArray());
    }

    private static uint[] AddMagnitudes(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        int maxLen = Math.Max(a.Length, b.Length);
        uint[] result = new uint[maxLen + 1];
        ulong carry = 0;

        for (int i = 0; i < maxLen; i++) {
            ulong sum = carry;
            if (i < a.Length) sum += a[i];
            if (i < b.Length) sum += b[i];
            result[i] = (uint)sum;
            carry = sum >> 32;
        }
        result[maxLen] = (uint)carry;

        return result;
    }

    private static uint[] SubtractMagnitudes(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        uint[] result = new uint[a.Length];
        long borrow = 0;

        for (int i = 0; i < a.Length; i++) {
            long diff = (long)a[i] - borrow;
            if (i < b.Length) {
                diff -= b[i];
            }
            if (diff < 0) {
                diff += 0x100000000L;
                borrow = 1;
            }
            else {
                borrow = 0;
            }
            result[i] = (uint)diff;
        }

        return result;
    }

    private static bool IsZero(BetterBigInteger a)
    {
        ReadOnlySpan<uint> digits = a.GetDigits();
        return digits.Length == 1 && digits[0] == 0;
    }
}
