﻿/*
The MIT License (MIT)

Copyright (c) 2015 Calvin Baart

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;

public static class Util
{
    /// <summary>
    ///  Convert a 16-bit value to LittleEndian
    /// </summary>
    /// <param name="val">The value to convert</param>
    /// <returns>The converted value</returns>
    public static short ConvertToLittle(short val)
    {
        byte[] intAsBytes = BitConverter.GetBytes(val);
        if(BitConverter.IsLittleEndian)
            Array.Reverse(intAsBytes);

        return BitConverter.ToInt16(intAsBytes, 0);
    }

    /// <summary>
    ///  Convert a 32-bit value to LittleEndian
    /// </summary>
    /// <param name="val">The value to convert</param>
    /// <returns>The converted value</returns>
    public static int ConvertToLittle(int val)
    {
        byte[] intAsBytes = BitConverter.GetBytes(val);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(intAsBytes);

        return BitConverter.ToInt32(intAsBytes, 0);
    }
}
