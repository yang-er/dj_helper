# Transfer

这次我们要传输个真榜（雾

配合 Board 项目使用。

```bash
./Transfer [domserver_url] [contest_id] [boarl_url] [auth_string] [delay_length_ms]
```

参数解释：

- domserver_url 当为http://[servername:port]/domjudge时可以直接写成[servername:port]
- contest_id 请参见/api/v4/contests里的那个contest_id
- board_url 榜单的地址，当为http://[servername:port]时可以直接写成[servername:port]
- auth_string 你设置的basic authorization用户名密码，格式[username]:[password]
- delay_length_ms 每次刷榜之间的间隔

例如，同步domjudge的demoweb的nwerc18：

```bash
./Transfer https://www.domjudge.org/demoweb nwerc18 https://board.keji.moe:81 [auth_string] 15000
```

例如，同步本地服务器：

```bash
./Transfer localhost 2 localhost:5000 15000
```

