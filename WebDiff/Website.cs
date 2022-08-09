using System;
using System.Security.Cryptography;
using System.Text;

namespace WebDiff {
    class Website {
        public string URL { get; set; }

        internal ulong HashCode {
            get {
                if (!hashCode.HasValue) {
                    MD5 hasher = MD5.Create();
                    hashCode = BitConverter.ToUInt64(hasher.ComputeHash(Encoding.UTF8.GetBytes(URL)));
                }
                return hashCode.Value;
            }
        }
        ulong? hashCode;

        public Website() { }

        public Website(string url) => URL = url;

        public override string ToString() => URL;
    }
}