using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ReconnectVPN.Helpers;
using ReconnectVPN.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using YamlDotNet.Serialization;

namespace ReconnectVPN.ViewModels
{
    public class MainWindowViewModel : BindableBase, IDisposable
    {
        private CompositeDisposable _disposable = new();
        private bool disposedValue;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private const string CONFIG_YML = "config.yml";

        public ReactivePropertySlim<string> Title { get; } = new();
        public ReactivePropertySlim<string> VPNName { get; } = new();
        public ReactivePropertySlim<string> Username { get; } = new();
        private ReactivePropertySlim<string> Password { get; } = new();
        public ReactivePropertySlim<string> SwitchMonitoringButtonCaption { get; } = new();
        public ReactivePropertySlim<bool> IsChecked { get; } = new();
        public ReactiveCommand<RoutedEventArgs> PasswordChangedCommand { get; }
        public ReactiveCommand SwitchMonitoringCommand { get; }
        public ReactiveCommand CallWindowsHelloCommand { get; }

        public MainWindowViewModel()
        {
            LoadYml();
            Title.Value = "ReconnectVPN";
            PasswordChangedCommand = new ReactiveCommand<RoutedEventArgs>().WithSubscribe(x =>
            {
                var passwordBox = App.Current.MainWindow.FindName("passwordBox") as PasswordBox;
                Password.Value = passwordBox.Password;
            })
            .AddTo(_disposable);
            SwitchMonitoringCommand = new ReactiveCommand().WithSubscribe(() =>
            {
                if (IsChecked.Value)
                {
                    Task.Factory.StartNew(async () =>
                    {
                        while (true)
                        {
                            if (_cancellationTokenSource.IsCancellationRequested)
                            {
                                break;
                            }

                            bool isConnected = CheckVPNConnection(VPNName.Value);

                            if (!isConnected)
                            {
                                Console.WriteLine("VPN接続が切断されました。再接続を試みます...");
                                bool isReconnected = ReconnectVPN(VPNName.Value, Username.Value, Password.Value);

                                if (isReconnected)
                                {
                                    Console.WriteLine("VPN接続が再接続されました。");
                                    SaveYml(VPNName.Value, Username.Value);
                                    PasswordManager.SetPassword(VPNName.Value, Username.Value, Password.Value);
                                }
                                else
                                {
                                    Console.WriteLine("VPN接続の再接続に失敗しました。");
                                }
                            }

                            //5秒間隔で動作
                            await Task.Delay(5000);
                        }
                    }, _cancellationTokenSource.Token);

                    SwitchMonitoringButtonCaption.Value = "End Monitoring";
                }
                else
                {
                    _cancellationTokenSource.Cancel();
                    SwitchMonitoringButtonCaption.Value = "Begin Monitoring";
                }
            })
            .AddTo(_disposable);
            CallWindowsHelloCommand = new ReactiveCommand().WithSubscribe(async () =>
            {
                var passwordBox = App.Current.MainWindow.FindName("passwordBox") as PasswordBox;
                passwordBox.Password = await PasswordManager.SignInAsync(VPNName.Value, Username.Value);
            })
            .AddTo(_disposable);
            SwitchMonitoringButtonCaption.Value = "Begin Monitoring";
        }

        private void LoadYml()
        {
            if (!File.Exists(CONFIG_YML))
                return;
            var vpnConnectionInfo = Deserialize(CONFIG_YML);
            VPNName.Value = vpnConnectionInfo.Name;
            Username.Value = vpnConnectionInfo.Username;
        }

        private static VpnConnectionInfo Deserialize(string yamlPath)
        {
            // テキスト抽出
            var input = new StreamReader(yamlPath, Encoding.UTF8);

            // デシリアライザインスタンス作成
            var deserializer = new Deserializer();

            // yamlデータのオブジェクトを作成
            var deserializeObject = deserializer.Deserialize<VpnConnectionInfo>(input);

            return deserializeObject;
        }

        private void SaveYml(string vpnName, string username)
        {
            var obj = new VpnConnectionInfo()
            {
                Name = vpnName,
                Username = username,
            };

            using TextWriter writer = File.CreateText(CONFIG_YML);
            var serializer = new Serializer();
            serializer.Serialize(writer, obj);
        }

        static bool CheckVPNConnection(string vpnName)
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface adapter in interfaces)
            {
                if (adapter.Name == vpnName && adapter.OperationalStatus == OperationalStatus.Up)
                {
                    return true;
                }
            }

            return false;
        }

        static bool ReconnectVPN(string vpnName, string username, string password)
        {
            Process process = new Process();
            process.StartInfo.FileName = "rasdial.exe";
            process.StartInfo.Arguments = string.Format("\"{0}\" {1} {2}", vpnName, username, password);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージド状態を破棄します (マネージド オブジェクト)
                    _disposable.Dispose();
                    Title.Dispose();
                    VPNName.Dispose();
                    Username.Dispose();
                    Password.Dispose();
                    SwitchMonitoringButtonCaption.Dispose();
                    IsChecked.Dispose();
                    PasswordChangedCommand.Dispose();
                    SwitchMonitoringCommand.Dispose();
                    CallWindowsHelloCommand.Dispose();
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                // TODO: 大きなフィールドを null に設定します
                _disposable = null;
                disposedValue = true;
            }
        }

        // // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
        // ~MainWindowViewModel()
        // {
        //     // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
