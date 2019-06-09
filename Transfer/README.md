# Transfer

这次我们要传输个真榜（雾

配合 Board 项目使用。

```bash
./Transfer [domserver_url] [contest_id] [boarl_url] [auth_string] [delay_length_ms]
```

参数解释：

- domserver_url 当为 `http://[servername:port]/domjudge` 时可以直接写成`[servername:port]`
- contest_id 请参见 `/api/v4/contests` 里的那个 `contest_id`
- board_url 榜单的地址，当为 `http://[servername:port]` 时可以直接写成 `[servername:port]`
- auth_string 你设置的 `basic authorization` 用户名密码，格式 `username:password`，默认就是这个，你要放公网的话具体在 `Board\Services\AuthorizationService.cs` 修改。
- delay_length_ms 每次刷榜之间的间隔

例如，同步domjudge的demoweb的nwerc18：

```bash
./Transfer https://www.domjudge.org/demoweb nwerc18 https://board.keji.moe:81 [auth_string] 15000
```

例如，同步本地服务器：

```bash
./Transfer localhost 2 localhost:5000 username:password 15000
```

如果你下载的不是二进制文件，而是准备用.NET Core环境运行的话，请将上面命令的 `./Transfer` 改为 `dotnet run`，就可以啦~