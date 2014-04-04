using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shapeshifter.Desktop.Functionality.Clipboard.Data;
using Shapeshifter.Desktop.Functionality.Clipboard.DataTypes;
using Shapeshifter.Desktop.Functionality.Clipboard.Factories;
using Shapeshifter.Desktop.Functionality.Clipboard.Session;
using Shapeshifter.Desktop.Functionality.Factories;

namespace Shapeshifter.Tests
{
    [TestClass]
    public class ClipboardTest
    {
        [TestMethod]
        public void ClipboardFetchTextItemTest()
        {
            string dummyString = Guid.NewGuid() + "";

            Clipboard.Clear();
            Clipboard.SetText(dummyString);

            var factory = new ClipboardTextFactory();

            var item = factory.CreateNewClipboardItem(IntPtr.Zero) as ClipboardText;
            Assert.IsNotNull(item);

            Assert.AreEqual(item.Text, dummyString);
        }

        [TestMethod]
        public void ClipboardFetchFileItemTest()
        {
            string file = Path.GetTempFileName(); //brug GetTempFileName i stedet for. den er nice. den laver selv en temp fil, og returnerer path'en.

            var dummyFiles = new StringCollection();
            dummyFiles.Add(file);
            Clipboard.Clear();
            Clipboard.SetFileDropList(dummyFiles);

            var factory = new ClipboardFileFactory();

            var item = factory.CreateNewClipboardItem(IntPtr.Zero) as ClipboardFile;
            Assert.IsNotNull(item);

            Assert.AreEqual(item.Path, dummyFiles[0]);
            File.Delete(file);
        }

        [TestMethod]
        public void ClipboardFetchFilesItemTest()
        {
            string file1 = Path.GetTempFileName();
            string file2 = Path.GetTempFileName();

            var dummyFiles = new StringCollection();

            dummyFiles.Add(file1);
            dummyFiles.Add(file2);

            Clipboard.Clear();
            Clipboard.SetFileDropList(dummyFiles);

            StringCollection copiedList = Clipboard.GetFileDropList();
            Assert.AreEqual(copiedList.Count, dummyFiles.Count);

            var factory = new ClipboardFileFactory();

            var item = factory.CreateNewClipboardItem(IntPtr.Zero) as ClipboardFileCollection;
            Assert.IsNotNull(item);

            Assert.AreEqual(item.Paths.Count(), 2);

            List<string> localList = item.Paths.ToList();

            for (int i = 0; i < localList.Count; i++)
            {
                Assert.AreEqual(localList[i], dummyFiles[i]);
            }

            File.Delete(file1);
            File.Delete(file2);
        }

        [TestMethod]
        public void ClearClipboardTest()
        {
            string data1 = ClearClipboard();
            string data2 = ClearClipboard();

            Assert.AreEqual(data1, data2);
        }

        private string ClearClipboard()
        {
            string text = Guid.NewGuid() + "";
            Clipboard.SetText(text);

            Assert.AreEqual(text, Clipboard.GetText());

            ClipboardSnapshot beforeSnapshot = ClipboardSnapshot.CreateSnapshot(IntPtr.Zero);
            Assert.IsTrue(beforeSnapshot.ContainsData);

            using (var session = new ClipboardSession(IntPtr.Zero))
            {
                session.ClearClipboard();
            }

            ClipboardSnapshot afterSnapshot = ClipboardSnapshot.CreateSnapshot(IntPtr.Zero);
            Assert.IsFalse(afterSnapshot.ContainsData);
            Assert.IsFalse(afterSnapshot.HasFormat(KnownClipboardFormats.CF_TEXT));

            Assert.AreNotEqual(text, Clipboard.GetText());

            return Clipboard.GetText();
        }

        [TestMethod]
        public void FetchDataTest()
        {
            string data1 = FetchData();
            string data2 = FetchData();

            Assert.AreNotEqual(data1, data2);
        }

        private string FetchData()
        {
            string text = Guid.NewGuid() + "";
            Clipboard.SetText(text);

            Assert.AreEqual(text, Clipboard.GetText());

            ClipboardSnapshot beforeSnapshot = ClipboardSnapshot.CreateSnapshot(IntPtr.Zero);
            Assert.IsTrue(beforeSnapshot.ContainsData);
            Assert.IsTrue(beforeSnapshot.HasFormat(KnownClipboardFormats.CF_UNICODETEXT));

            using (var session = new ClipboardSession(IntPtr.Zero))
            {
                session.ClearClipboard();
            }

            IntPtr dataPointer = beforeSnapshot.FetchDataPointer(KnownClipboardFormats.CF_UNICODETEXT);
            string clipboardText = Marshal.PtrToStringAuto(dataPointer);

            Assert.AreEqual(text, clipboardText);

            return clipboardText;
        }
    }
}