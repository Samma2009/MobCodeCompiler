﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobCode
{
    public static class FileTree
    {
        public static List<FTEntry> s = new();
    }

    public abstract class FTEntry
    {
        public string name;
    }

    public class DirEntry : FTEntry
    {
        public List<FTEntry> data = new();
    }

    public class FileEntry : FTEntry
    {
        public string data;
    }
}
