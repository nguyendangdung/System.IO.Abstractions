using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;

namespace TestingHelpers.Fix
{
    public class MockFileSystemFixIssues : MockFileSystem
    {
        public MockFileSystemFixIssues(IDictionary<string, MockFileData> files, string currentDirectory = "") 
            : base(files, currentDirectory)
        {
            if (files?.Any() != true) return;
            if (files.Any(s => !s.Value.IsDirectory && s.Key.EndsWith(MockUnixSupport.Separator())))
            {
                throw new Exception("File path can't ends with Separator character");
            }

            var files1 = files.Select(s =>
            {
                if (s.Key.EndsWith(MockUnixSupport.Separator()))
                {
                    return new KeyValuePair<string, MockFileData>(s.Key.Substring(0, s.Key.Length - 1), s.Value);
                }
                return new KeyValuePair<string, MockFileData>(s.Key, s.Value);
            });

            if (files1.GroupBy(s => s.Key, (key, list) => new { key, list }).Any(s => s.list.Count() > 1))
            {
                throw new Exception();
            }
        }

        public override void AddFile(string path, MockFileData mockFile)
        {
            var fixedPath = FixPath(path);
            var separator = MockUnixSupport.Separator();
            if (fixedPath.EndsWith(separator))
            {
                fixedPath = fixedPath.Substring(0, fixedPath.Length - 1);
            }
            lock (files)
            {
                if (FileExists(fixedPath))
                {
                    var isReadOnly = (files[fixedPath].Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
                    var isHidden = (files[fixedPath].Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;

                    if (isReadOnly || isHidden)
                    {
                        throw new UnauthorizedAccessException(string.Format(CultureInfo.InvariantCulture, System.IO.Abstractions.TestingHelpers.Properties.Resources.ACCESS_TO_THE_PATH_IS_DENIED, path));
                    }
                }
                var directoryPath = Path.GetDirectoryName(fixedPath);
                if (!directory.Exists(directoryPath))
                {
                    AddDirectory(directoryPath);
                }
                files[fixedPath] = mockFile;
            }
        }

        public override void AddDirectory(string path)
        {
            var fixedPath = FixPath(path);
            var separator = MockUnixSupport.Separator();
            if (fixedPath.EndsWith(separator))
            {
                fixedPath = fixedPath.Substring(0, fixedPath.Length - 1);
            }
            lock (files)
            {
                if (FileExists(fixedPath) &&
                    (files[fixedPath].Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    throw new UnauthorizedAccessException(string.Format(CultureInfo.InvariantCulture, System.IO.Abstractions.TestingHelpers.Properties.Resources.ACCESS_TO_THE_PATH_IS_DENIED, path));
                if (FileExists(fixedPath))
                {
                    throw new UnauthorizedAccessException(string.Format(CultureInfo.InvariantCulture, System.IO.Abstractions.TestingHelpers.Properties.Resources.ACCESS_TO_THE_PATH_IS_DENIED, path));
                }
                var lastIndex = 0;

                bool isUnc =
                    fixedPath.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase) ||
                    fixedPath.StartsWith(@"//", StringComparison.OrdinalIgnoreCase);

                if (isUnc)
                {
                    //First, confirm they aren't trying to create '\\server\'
                    lastIndex = fixedPath.IndexOf(separator, 2, StringComparison.OrdinalIgnoreCase);
                    if (lastIndex < 0)
                        throw new ArgumentException(@"The UNC path should be of the form \\server\share.", "path");

                    /*
                     * Although CreateDirectory(@"\\server\share\") is not going to work in real code, we allow it here for the purposes of setting up test doubles.
                     * See PR https://github.com/tathamoddie/System.IO.Abstractions/pull/90 for conversation
                     */
                }

                while ((lastIndex = fixedPath.IndexOf(separator, lastIndex + 1, StringComparison.OrdinalIgnoreCase)) > -1)
                {
                    var segment = fixedPath.Substring(0, lastIndex + 1);
                    if (!directory.Exists(segment))
                    {
                        files[segment] = new MockDirectoryData();
                    }
                }

                var s = fixedPath.EndsWith(separator, StringComparison.OrdinalIgnoreCase) ? fixedPath : fixedPath + separator;
                files[s] = new MockDirectoryData();
            }
        }
    }
}
