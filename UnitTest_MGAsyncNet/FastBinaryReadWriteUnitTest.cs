using Microsoft.VisualStudio.TestTools.UnitTesting;

using MGAsyncNet;

namespace UnitTest_MGAsyncNet
{
    [TestClass]
    public class FastBinaryReadWriteUnitTest
    {
        [TestMethod]
        public void TestMethod_Int16()
        {
            var buffer1 = new byte[128];
            short value = 23456;
            var testBuf = System.BitConverter.GetBytes(value);

            FastBinaryReadWrite.WriteInt16(ref buffer1, 0, value);

            Assert.AreEqual(buffer1[0], testBuf[0]);
            Assert.AreEqual(buffer1[1], testBuf[1]);
        }


        [TestMethod]
        public void TestMethod_Single()
        {
            var buffer1 = new byte[128];
            float value = 23456.123f;
            var testBuf = System.BitConverter.GetBytes(value);

            FastBinaryReadWrite.WriteSingle(ref buffer1, 0, value);

            Assert.AreEqual(buffer1[0], testBuf[0]);
            Assert.AreEqual(buffer1[1], testBuf[1]);
            Assert.AreEqual(buffer1[2], testBuf[2]);
            Assert.AreEqual(buffer1[3], testBuf[3]);
        }
    }
}
