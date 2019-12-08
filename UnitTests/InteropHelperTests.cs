// Copyright(c) 2015-2019 Melvyn Laïly
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
using System;
using System.Diagnostics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scrobbling;
using XmpSharpScrobbler.Misc;

namespace UnitTests
{
    [TestClass]
    public class InteropHelperTests
    {
        private const int BufferSize = 5;
        private readonly byte[] _testBackingField = new byte[BufferSize];

        private void InitializeBufferForTest()
        {
            _testBackingField[0] = 42;
            _testBackingField[1] = 42;
            _testBackingField[2] = 42;
            _testBackingField[3] = 42;
            _testBackingField[4] = 42;

            // Sanity checks
            Assert.AreEqual(BufferSize, _testBackingField.Length);
            CollectionAssert.AreEquivalent(new byte[] { 42, 42, 42, 42, 42 }, _testBackingField);
        }

        [TestMethod]
        public void SetNativeString_Writes_ASCII_To_Backing_Field()
        {
            // Arrange

            InitializeBufferForTest();
            var value = "ab";

            // Act

            InteropHelper.SetNativeString(_testBackingField, Encoding.ASCII, value);

            // Assert

            Assert.IsNotNull(_testBackingField);
            Assert.AreEqual(BufferSize, _testBackingField.Length);
            CollectionAssert.AreEquivalent(new byte[] { 97, 98, 0, 0, 0 }, _testBackingField);
        }

        [TestMethod]
        public void SetNativeString_Writes_UTF16_To_Backing_Field()
        {
            // Arrange

            InitializeBufferForTest();
            var value = "ab";

            // Act

            InteropHelper.SetNativeString(_testBackingField, Encoding.Unicode, value);

            // Assert

            Assert.IsNotNull(_testBackingField);
            Assert.AreEqual(BufferSize, _testBackingField.Length);
            CollectionAssert.AreEquivalent(new byte[] { 97, 0, 98, 0, 0 }, _testBackingField);
        }

        [TestMethod]
        public void SetNativeString_Clears_Backing_Field_When_Value_Null()
        {
            // Arrange

            InitializeBufferForTest();
            string value = null;

            // Act

            InteropHelper.SetNativeString(_testBackingField, Encoding.ASCII, value);

            // Assert

            Assert.IsNotNull(_testBackingField);
            Assert.AreEqual(BufferSize, _testBackingField.Length);
            CollectionAssert.AreEquivalent(new byte[] { 0, 0, 0, 0, 0 }, _testBackingField);
        }

        [TestMethod]
        public void SetNativeString_Clears_Backing_Field_When_Value_Empty()
        {
            // Arrange

            InitializeBufferForTest();
            string value = "";

            // Act

            InteropHelper.SetNativeString(_testBackingField, Encoding.ASCII, value);

            // Assert

            Assert.IsNotNull(_testBackingField);
            Assert.AreEqual(BufferSize, _testBackingField.Length);
            CollectionAssert.AreEquivalent(new byte[] { 0, 0, 0, 0, 0 }, _testBackingField);
        }

        [TestMethod]
        public void SetNativeString_Clears_Backing_Field_When_Value_Null_Chars()
        {
            // Arrange

            InitializeBufferForTest();
            string value = "\0\0\0";

            // Act

            InteropHelper.SetNativeString(_testBackingField, Encoding.ASCII, value);

            // Assert

            Assert.IsNotNull(_testBackingField);
            Assert.AreEqual(BufferSize, _testBackingField.Length);
            CollectionAssert.AreEquivalent(new byte[] { 0, 0, 0, 0, 0 }, _testBackingField);
        }

        [TestMethod]
        public void SetNativeString_Defaults_To_Clearing_Backing_Field_When_Value_Too_Large()
        {
            // Arrange

            InitializeBufferForTest();
            var value = "abcdefgh";

            // Act

            InteropHelper.SetNativeString(_testBackingField, Encoding.ASCII, value);

            // Assert

            Assert.IsNotNull(_testBackingField);
            Assert.AreEqual(BufferSize, _testBackingField.Length);
            CollectionAssert.AreEquivalent(new byte[] { 0, 0, 0, 0, 0 }, _testBackingField);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SetNativeString_Throws_If_Requested_When_Value_Too_Large()
        {
            // Arrange

            InitializeBufferForTest();
            var value = "abcdefgh";

            // Act

            InteropHelper.SetNativeString(_testBackingField, Encoding.ASCII, value, throwOnBufferTooSmall: true);

            // Assert

            Assert.Fail("Expected to throw.");
        }

        [TestMethod]
        public void GetStringFromNativeBuffer_Returns_Expected_ASCII_Value()
        {
            // Arrange

            var buffer = new byte[] { 97, 98, 99, 0, 0 };
            // Act

            var actual = InteropHelper.GetStringFromNativeBuffer(buffer, Encoding.ASCII);

            // Assert

            Assert.AreEqual("abc", actual);
        }

        [TestMethod]
        public void GetStringFromNativeBuffer_Returns_Expected_ASCII_Value_For_Max_Buffer_Length()
        {
            // Arrange

            var buffer = new byte[] { 97, 98, 99, 100, 101 };

            // Act

            var actual = InteropHelper.GetStringFromNativeBuffer(buffer, Encoding.ASCII);

            // Assert

            Assert.AreEqual("abcde", actual);
        }

        [TestMethod]
        public void GetStringFromNativeBuffer_Returns_Expected_ASCII_Value_For_Empty_String()
        {
            // Arrange

            var buffer = new byte[] { 0, 0, 0, 0, 0 };
            // Act

            var actual = InteropHelper.GetStringFromNativeBuffer(buffer, Encoding.ASCII);

            // Assert

            Assert.AreEqual("", actual);
        }

        [TestMethod]
        public void GetStringFromNativeBuffer_Returns_Expected_UTF8_Value()
        {
            // Arrange

            var buffer = new byte[] { 97, 98, 99, 0, 0 };

            // Act

            var actual = InteropHelper.GetStringFromNativeBuffer(buffer, Encoding.UTF8);

            // Assert

            Assert.AreEqual("abc", actual);
        }

        [TestMethod]
        public void GetStringFromNativeBuffer_Returns_Expected_UTF8_Value_For_Max_Buffer_Length()
        {
            // Arrange

            var buffer = new byte[] { 97, 98, 99, 100, 101 };

            // Act

            var actual = InteropHelper.GetStringFromNativeBuffer(buffer, Encoding.UTF8);

            // Assert

            Assert.AreEqual("abcde", actual);
        }

        [TestMethod]
        public void GetStringFromNativeBuffer_Returns_Expected_UTF16_Value_For_Empty_String()
        {
            // Arrange

            var buffer = new byte[] { 0, 0, 0, 0, 0 };

            // Act

            var actual = InteropHelper.GetStringFromNativeBuffer(buffer, Encoding.Unicode);

            // Assert

            Assert.AreEqual("", actual);
        }

        [TestMethod]
        public void GetStringFromNativeBuffer_Returns_Expected_UTF16_Value()
        {
            // Arrange

            var buffer = new byte[] { 97, 0, 98, 0, 0, 0 };

            // Act

            var actual = InteropHelper.GetStringFromNativeBuffer(buffer, Encoding.Unicode);

            // Assert

            Assert.AreEqual("ab", actual);
        }

        [TestMethod]
        public void GetStringFromNativeBuffer_Returns_Expected_UTF32_Value()
        {
            // Arrange

            // 4 bytes per character
            var buffer = new byte[] { 97, 0, 0, 0, 0 };

            // Act

            var actual = InteropHelper.GetStringFromNativeBuffer(buffer, Encoding.UTF32);

            // Assert

            Assert.AreEqual("a", actual);
        }

    }
}
