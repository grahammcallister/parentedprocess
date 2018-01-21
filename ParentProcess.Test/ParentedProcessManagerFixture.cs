using System;
using System.IO;
using NUnit.Framework;

namespace ParentProcess.Test
{
    [TestFixture]
    public class ParentedProcessManagerFixture
    {
        [TestCase]
        public void ParentProcessManagerConstructor_WithNullFilename_ThrowsNullException()
        {
            // Arrange + Act + Assert
            Assert.Throws<ArgumentNullException>(() => { new ParentedProcessManager(null, null); });
        }

        [TestCase]
        public void ParentProcessManagerConstructor_WithEmptyFilename_ThrowsNullException()
        {
            // Arrange + Act + Assert
            Assert.Throws<ArgumentNullException>(() => { new ParentedProcessManager(string.Empty, null); });
        }

        [TestCase]
        public void ParentProcessManagerConstructor_WithNotFoundFilename_ThrowsNullException()
        {
            // Arrange + Act + Assert
            Assert.Throws<FileNotFoundException>(() => { new ParentedProcessManager(@"C:\Foo\Bar.exe", null); });
        }
    }
}