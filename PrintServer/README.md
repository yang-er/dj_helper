# Print Server

为了解决DOMjudge的打印分发，写的一个程序。

运行原理是搭建一个HTTP服务，GET可以获得队列任务数，POST可以提交打印任务。

虽然感觉好像确实可以用lpr自己解决这个问题啊（大雾）

然后打印的部分是，将任务发送到打印队列里，等到打印队列空了继续处理下一个任务，任务全部缓存在内存中。

Windows版需要下载ghostscript，然后将`gsdll64.dll`、`gsdll64.lib`、`gswin64.exe`、`gswin64c.exe`复制到运行文件夹中，并安装上XPS Viewer，就可以处理了，每次来打印任务需要手动点一下打印哦（因为Win32API太难调了，我还是没调试通过，有兴趣的可以研究一下XPS发送至打印队列啊）。

Linux版使用`lpr`和`lpq`处理。

运行打包好的请

```bash
./PrintServer 5000
```

表示监听在5000端口，然后就可以负载均衡啦~