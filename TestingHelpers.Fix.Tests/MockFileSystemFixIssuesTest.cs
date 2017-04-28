using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO.Abstractions.TestingHelpers;

namespace TestingHelpers.Fix.Tests
{
    [TestFixture]
    public class MockFileSystemFixIssuesTest
    {
        [Test]
        public void MockFileStream_Create_Dir_And_File_Same_Name()
        {
            var filesystem = new MockFileSystemFixIssues(new Dictionary<string, MockFileData>());
            filesystem.AddFile(@"c:\foo.txt", MockFileData.NullObject);
            filesystem.AddFile(@"c:\foo", MockFileData.NullObject);
            Assert.AreEqual(filesystem.Directory.GetFiles("c:\\").Length, 2);
            Assert.Throws<UnauthorizedAccessException>(() => filesystem.AddDirectory(@"c:\foo"));
            Assert.Throws<UnauthorizedAccessException>(() => filesystem.AddDirectory(@"c:\foo\"));
        }

        [Test]
        public void MockFileStream_wrong_init_data_file_path_end_with_separator()
        {
            Assert.Throws<Exception>(() =>
            {
                var filesystem = new MockFileSystem(new Dictionary<string, MockFileData>()
                {
                    {"C\\test.txt", MockFileData.NullObject},
                    {"C\\test", MockFileData.NullObject},
                    {"C\\bar", MockFileData.NullObject},
                    {"C:\\foo\\", MockFileData.NullObject},
                });
            });
        }

        [Test]
        public void MockFileStream_wrong_init_data_file_dir_same_name()
        {
            Assert.Throws<Exception>(() =>
            {
                var filesystem = new MockFileSystem(new Dictionary<string, MockFileData>()
                {
                    {"C\\test.txt", MockFileData.NullObject},
                    {"C\\test", MockFileData.NullObject},
                    {"C\\bar", MockFileData.NullObject},
                    {"C:\\foo", MockFileData.NullObject},
                    {"C:\\foo\\", new MockDirectoryData()},
                });
            });
        }
    }
}
