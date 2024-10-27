﻿using System;

namespace L4D2AddonAssistant
{
    public class MoveFileException : Exception
    {
        public MoveFileException(string sourcePath, string targetPath, Exception? innerException = null) : base(null, innerException)
        {
            SourcePath = sourcePath;
            TargetPath = targetPath;
        }

        public string SourcePath { get; }

        public string TargetPath { get; }
    }
}
