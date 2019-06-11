# systemd 脚本

由于不太会用 supervisord，而对 systemd 这套比较熟悉，于是就写了这一套东西。

毕竟现在Ubuntu 18.04开始都是带这个的，非常方便。

注意更改user=group=[你的用户名]，还有/opt/domjudge为你真实安装目录。

```bash
ln -s /opt/domjudge/judgehost/etc/create_cgroups.service /etc/systemd/system/
ln -s /opt/domjudge/judgehost/etc/judgehost-x.service /etc/systemd/system/
systemctl enable create_cgroups
systemctl start create_cgroups
systemctl enable judgehost-x
systemctl start judgehost-x
```

这样设置了开机自动create_cgroups和启动judgehost。自行复制过去和编辑吧~

另外如果judgehost部署在另外的机器上的话可以把After=apache2.service删除。