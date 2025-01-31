﻿namespace NearClientUnity.Providers
{
    public abstract class ChunkResult
    {
        public abstract ChunkHeader Header { get; set; }
        public abstract object[] Receipts { get; set; }
        public abstract Transaction[] Transactions { get; set; }
    }
}