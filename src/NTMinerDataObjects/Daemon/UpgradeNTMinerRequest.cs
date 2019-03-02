﻿using System.Text;

namespace NTMiner.Daemon {
    public class UpgradeNTMinerRequest : RequestBase, ISignatureRequest {
        public UpgradeNTMinerRequest() { }
        public string LoginName { get; set; }
        public string NTMinerFileName { get; set; }
        public string Sign { get; set; }

        public void SignIt(string password) {
            this.Sign = this.GetSign(password);
        }

        public string GetSign(string password) {
            StringBuilder sb = new StringBuilder();
            sb.Append(nameof(MessageId)).Append(MessageId)
                .Append(nameof(LoginName)).Append(LoginName)
                .Append(nameof(NTMinerFileName)).Append(NTMinerFileName)
                .Append(nameof(Timestamp)).Append(Timestamp.ToUlong())
                .Append(nameof(UserData.Password)).Append(password);
            return HashUtil.Sha1(sb.ToString());
        }
    }
}
