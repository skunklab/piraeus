﻿using System.Threading.Tasks;

namespace SkunkLab.Storage
{
    public abstract class PskStorageAdapter
    {
        public abstract Task<string[]> GetKeys();

        public abstract Task<string> GetSecretAsync(string key);

        public abstract Task RemoveSecretAsync(string key);

        public abstract Task SetSecretAsync(string key, string value);
    }
}