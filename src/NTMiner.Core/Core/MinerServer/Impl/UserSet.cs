﻿using LiteDB;
using NTMiner.User;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NTMiner.Core.MinerServer.Impl {
    public class UserSet : IUserSet {
        private Dictionary<string, UserData> _dicByLoginName = new Dictionary<string, UserData>();

        public UserSet() {
            VirtualRoot.Accept<AddUserCommand>(
                "处理添加用户命令",
                LogEnum.Console,
                action: message => {
                    if (!_dicByLoginName.ContainsKey(message.User.LoginName)) {
                        Server.ControlCenterService.AddUserAsync(new UserData {
                            LoginName = message.User.LoginName,
                            Password = message.User.Password,
                            IsEnabled = message.User.IsEnabled,
                            Description = message.User.Description
                        }, (response, exception) => {
                            if (response.IsSuccess()) {
                                UserData entity = new UserData(message.User);
                                _dicByLoginName.Add(message.User.LoginName, entity);
                                VirtualRoot.Happened(new UserAddedEvent(entity));
                            }
                            else if (response != null) {
                                Write.UserLine(response.Description, ConsoleColor.Red);
                            }
                        });
                    }
                });
            VirtualRoot.Accept<UpdateUserCommand>(
                "处理修改用户命令",
                LogEnum.Console,
                action: message => {
                    if (_dicByLoginName.ContainsKey(message.User.LoginName)) {
                        UserData entity = _dicByLoginName[message.User.LoginName];
                        UserData oldValue = new UserData(entity);
                        entity.Update(message.User);
                        Server.ControlCenterService.UpdateUserAsync(new UserData {
                            LoginName = message.User.LoginName,
                            Password = message.User.Password,
                            IsEnabled = message.User.IsEnabled,
                            Description = message.User.Description
                        }, (response, exception) => {
                            if (!response.IsSuccess()) {
                                entity.Update(oldValue);
                                VirtualRoot.Happened(new UserUpdatedEvent(entity));
                                if (response != null) {
                                    Write.UserLine(response.Description, ConsoleColor.Red);
                                }
                            }
                        });
                        VirtualRoot.Happened(new UserUpdatedEvent(entity));
                    }
                });
            VirtualRoot.Accept<RemoveUserCommand>(
                "处理删除用户命令",
                LogEnum.Console,
                action: message => {
                    if (_dicByLoginName.ContainsKey(message.LoginName)) {
                        UserData entity = _dicByLoginName[message.LoginName];
                        Server.ControlCenterService.RemoveUserAsync(message.LoginName, (response, exception) => {
                            if (response.IsSuccess()) {
                                _dicByLoginName.Remove(entity.LoginName);
                                VirtualRoot.Happened(new UserRemovedEvent(entity));
                            }
                            else if (response != null) {
                                Write.UserLine(response.Description, ConsoleColor.Red);
                            }
                        });
                    }
                });
        }

        private object _locker = new object();
        private bool _isInited = false;

        private void InitOnece() {
            DateTime now = DateTime.Now;
            if (!_isInited) {
                lock (_locker) {
                    if (!_isInited) {
                        Guid? clientId = null;
                        if (!VirtualRoot.IsControlCenter) {
                            clientId = ClientId.Id;
                        }
                        var result = Server.ControlCenterService.GetUsers(clientId);
                        _dicByLoginName = result.ToDictionary(a => a.LoginName, a => a);
                        _isInited = true;
                    }
                }
            }
        }

        public void Refresh() {
            _dicByLoginName.Clear();
            _isInited = false;
        }

        public IUser GetUser(string loginName) {
            InitOnece();
            UserData userData;
            if (_dicByLoginName.TryGetValue(loginName, out userData)) {
                return userData;
            }
            return null;
        }

        public IEnumerator<IUser> GetEnumerator() {
            InitOnece();
            return _dicByLoginName.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            InitOnece();
            return _dicByLoginName.Values.GetEnumerator();
        }
    }
}