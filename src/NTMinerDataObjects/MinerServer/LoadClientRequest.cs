﻿using System;
using System.Text;

namespace NTMiner.MinerServer {
    public class LoadClientRequest : RequestBase, ISignatureRequest {
        public LoadClientRequest() { }
        public string LoginName { get; set; }
        public Guid ClientId { get; set; }
        public string Sign { get; set; }

        public void SignIt(string password) {
            this.Sign = this.GetSign(password);
        }

        public string GetSign(string password) {
            StringBuilder sb = GetSignData().Append(nameof(UserData.Password)).Append(password);
            return HashUtil.Sha1(sb.ToString());
        }

        public StringBuilder GetSignData() {
            StringBuilder sb = new StringBuilder();
            sb.Append(nameof(LoginName)).Append(LoginName)
                .Append(nameof(ClientId)).Append(ClientId)
                .Append(nameof(Timestamp)).Append(Timestamp.ToUlong());
            return sb;
        }
    }
}
