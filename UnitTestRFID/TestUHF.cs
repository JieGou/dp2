﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using DigitalPlatform.RFID;
using DigitalPlatform;
using System.Diagnostics;
using static DigitalPlatform.RFID.LogicChip;

namespace UnitTestRFID
{

    [TestClass]
    public class TestUHF
    {
        [TestMethod]
        public void Test_encode_longNumericString()
        {
            // 以 long numeric string 方式编码一个数字字符串
            var bytes = UhfUtility.EncodeLongNumericString("12312345678");

            byte[] correct = Element.FromHexString(
@"FB 21 02 DD DF 7C 4E");

            // 
            Assert.AreEqual(0, ByteArray.Compare(correct, bytes));
        }


        [TestMethod]
        public void Test_decode_longNumericString()
        {
            byte[] source = Element.FromHexString(
@"FB 21 02 DD DF 7C 4E");
            string result = UhfUtility.DecodeLongNumericString(source, 0, out int used);

            // 
            Assert.AreEqual("12312345678", result);
            Assert.AreEqual(source.Length, used);
        }

        // 测试编码 UII
        [TestMethod]
        public void Test_encode_uii_1()
        {
            TestEncodeUII("CH-", "141c");

            TestEncodeUII("000", "c04f");

            TestEncodeUII("134", "c70b");

            TestEncodeUII("-1.", "adb5");

            TestEncodeUII("123", "c6e2");

            TestEncodeUII("456", "da1d");

            TestEncodeUII("78.", "ed4d");

            TestEncodeUII("31", "d319");
        }

        static void TestEncodeUII(string uii, string hex)
        {
            var bytes = UhfUtility.EncodeUII(uii);

            byte[] correct = Element.FromHexString(hex);

            // 
            Assert.AreEqual(0, ByteArray.Compare(correct, bytes));
        }

        // 测试编码 UII
        // ISO/TS 28560-4:2014(E) page 49
        [TestMethod]
        public void Test_encode_uii_2()
        {
            var bytes = UhfUtility.EncodeUII("CH-000134-1.12345678.31");

            byte[] correct = Element.FromHexString(
@"141c c04f c70b adb5 c6e2 da1d ed4d d319");

            // 
            Assert.AreEqual(0, ByteArray.Compare(correct, bytes));
        }

        // page 17 of:
        // https://www.ipc.be/~/media/documents/public/operations/rfid/ipc%20rfid%20standard%20for%20test%20letters.pdf?la=en
        [TestMethod]
        public void Test_encode_uii_3()
        {
            var bytes = UhfUtility.EncodeUII("B.A12312345678");

            byte[] correct = Element.FromHexString(
@"10 E2 FB 21 02 DD DF 7C 4E");

            // 
            Assert.AreEqual(0, ByteArray.Compare(correct, bytes));
        }

        // 测试解码 UII
        [TestMethod]
        public void Test_decode_uii_1()
        {
            TestDecodeUii("141c", "CH-");
            TestDecodeUii("c04f", "000");
            TestDecodeUii("c70b", "134");
            TestDecodeUii("adb5", "-1.");
            TestDecodeUii("c6e2", "123");
            TestDecodeUii("da1d", "456");
            TestDecodeUii("ed4d", "78.");
            TestDecodeUii("d319", "31");
        }

        static void TestDecodeUii(string hex, string text)
        {
            byte[] source = Element.FromHexString(hex);

            var result = UhfUtility.DecodeUII(source, 0, source.Length);

            // 
            Assert.AreEqual(text, result);
        }

        // 测试解码 UII
        // ISO/TS 28560-4:2014(E) page 49
        [TestMethod]
        public void Test_decode_uii_2()
        {
            byte[] source = Element.FromHexString(
@"141c c04f c70b adb5 c6e2 da1d ed4d d319");

            var result = UhfUtility.DecodeUII(source, 0, source.Length);

            // 
            Assert.AreEqual("CH-000134-1.12345678.31", result);
        }

        // 9 字符才会形成 digit 类型
        [TestMethod]
        public void Test_splitSegment_1()
        {
            var segments = UhfUtility.SplitSegment("123456789");

            Assert.AreEqual(1, segments.Count);
            Assert.AreEqual("digit", segments[0].Type);
            Assert.AreEqual("123456789", segments[0].Text);
        }

        // 8 字符只能形成 table 类型
        [TestMethod]
        public void Test_splitSegment_2()
        {
            var segments = UhfUtility.SplitSegment("12345678");

            Assert.AreEqual(1, segments.Count);
            Assert.AreEqual("table", segments[0].Type);
            Assert.AreEqual("12345678", segments[0].Text);
        }

        [TestMethod]
        public void Test_splitSegment_3()
        {
            var segments = UhfUtility.SplitSegment("AB.123456789");

            Assert.AreEqual(2, segments.Count);

            Assert.AreEqual("table", segments[0].Type);
            Assert.AreEqual("AB.", segments[0].Text);

            Assert.AreEqual("digit", segments[1].Type);
            Assert.AreEqual("123456789", segments[1].Text);
        }

        [TestMethod]
        public void Test_splitSegment_4()
        {
            var segments = UhfUtility.SplitSegment("123456789AB.");

            Assert.AreEqual(2, segments.Count);

            Assert.AreEqual("digit", segments[0].Type);
            Assert.AreEqual("123456789", segments[0].Text);

            Assert.AreEqual("table", segments[1].Type);
            Assert.AreEqual("AB.", segments[1].Text);
        }

        [TestMethod]
        public void Test_splitSegment_5()
        {
            var segments = UhfUtility.SplitSegment("1234/56789");

            Assert.AreEqual(3, segments.Count);

            Assert.AreEqual("table", segments[0].Type);
            Assert.AreEqual("1234", segments[0].Text);

            Assert.AreEqual("utf8-one-byte", segments[1].Type);
            Assert.AreEqual("/", segments[1].Text);

            Assert.AreEqual("table", segments[2].Type);
            Assert.AreEqual("56789", segments[2].Text);
        }

        // 含有 UTF-8 汉字字符
        [TestMethod]
        public void Test_splitSegment_6()
        {
            var segments = UhfUtility.SplitSegment("1234中国56789");

            Assert.AreEqual(3, segments.Count);

            Assert.AreEqual("table", segments[0].Type);
            Assert.AreEqual("1234", segments[0].Text);

            Assert.AreEqual("utf8-triple-byte", segments[1].Type);
            Assert.AreEqual("中国", segments[1].Text);

            Assert.AreEqual("table", segments[2].Type);
            Assert.AreEqual("56789", segments[2].Text);
        }

        [TestMethod]
        public void Test_splitSegment_7()
        {
            var segments = UhfUtility.SplitSegment("78.");

            Assert.AreEqual(1, segments.Count);
            Assert.AreEqual("table", segments[0].Type);
            Assert.AreEqual("78.", segments[0].Text);
        }

        // 编码 MB11
        // ISO/TS 28560-4:2014(E) page 52
        [TestMethod]
        public void Test_encode_mb11_1()
        {
            LogicChip chip = new LogicChip();
            chip.NewElement(ElementOID.SetInformation, "1203");
            chip.NewElement(ElementOID.ShelfLocation, "QA268.L55");
            chip.NewElement(ElementOID.OwnerInstitution, "US-InU-Mu").CompactMethod = CompactionScheme.SevenBitCode;    // 如果让 GetBytes() 自动选择压缩方案，这个元素会被选择 ISIL 压缩方案
            Debug.Write(chip.ToString());

            var result = chip.GetBytes(4 * 9,
                4,
                GetBytesStyle.ReserveSequence,
                out string block_map);
            string result_string = Element.GetHexString(result, "4");
            byte[] correct = Element.FromHexString(
    @"02 01 D0 14 02
04B3 4607
441C b6E2
E335 D653
08AB 4D6C
9DD5 56CD
EB"
);
            Assert.IsTrue(result.SequenceEqual(correct));

            // Assert.AreEqual(block_map, "ww....www");

        }
    }
}
