# Board

我可能看了个假榜（雾

这是一个基于 ASP.NET Core MVC 2.2 写的假榜，可以用于比赛的时候给外网教练们看。

默认模式为显示学校图片，请将affiliations里学校short_name变成小写字母后，放入wwwroot/images/affiliations/{lowercase_short_name}.png。

更新使用了RESTful API / Authorization Basic，需要更改用户名密码请进入Services/AuthorizationServices.cs。

在自己的服务器上运行：

```bash
dotnet run
```

可以配合 Transfer 项目使用。

DEMO: [2019黑龙江省赛的假榜](https://board.keji.moe:81/) （可能吉林省赛和东北四省赛也要用？）
