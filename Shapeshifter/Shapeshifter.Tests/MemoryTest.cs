using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Shapeshifter.Tests
{
    [TestClass]
    public class MemoryTest
    {
        [TestMethod]
        public void AllocationTest()
        {
            string string1 = Guid.NewGuid() + "-1";
            string string2 = Guid.NewGuid() + "-2";

            byte[] bytes1 = Encoding.Default.GetBytes(string1);
            byte[] bytes2 = Encoding.Default.GetBytes(string2);

            IntPtr pointer1 = Marshal.AllocHGlobal(bytes1.Length);
            IntPtr pointer2 = Marshal.AllocHGlobal(bytes2.Length);

            Assert.AreNotEqual(pointer1, pointer2);
        }
    }
}