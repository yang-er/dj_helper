# 滚榜程序

其他的等有时间了再来继续研究吧！

那个jar包是基于 `resolver-2.0.1798.zip` 生成的，IDEA项目扔过来了，有能力的同学应该可以搞定怎么魔改其他的。

### 我的魔改

由于Resolver从1.3开始就不太支持中文，于是翻了翻源码……

发现这玩意被他改成了内嵌字体啊摔！ICPC_FONT那个环境变量并没有用。

然后就魔改了 `org.acmicpc.presentation.contest.internal.ICPCFont`

另外看着队名和学校一起的那个字符串有点不太爽

然后就魔改了 `org.acmicpc.presentation.contest.internal.TeamUtil`

感谢Java Decompiler的支持！（雾

以及强行指定 `ICPC_PRESENTATION_STYLE` 的话请改 `resolver.bat/sh`，在 `java -Xmx1024m` 中间加上参数 `-DICPC_PRESENTATION_STYLE=team_name`

### 打星队伍的处理

由于比赛的时候可能会有打星队伍，然后这个award程序也比较弟弟不太支持忽略……

那么，我们就先进入DOMjudge后台，将打星队伍的category设置为隐藏

进 `api/v4/contests/{cid}/event?stream=false` 下载滚榜数据

然后进award程序加奖项，再打开改完的文件，将最后几行能找到的 team update hidden true 删除，就能滚榜的时候既有打星队伍，又有正式队伍，并且不会给打星队伍颁发奖项了！

注意在Windows下那个award的GUI程序保存的文件是GB2312的，然而resolver读取的是UTF-8，拿Notepad++处理一下就行了？

虽然好像大家可以通过主持人圆滑的解决这个问题（雾

### 滚榜程序速度

这个你读一下文档就知道 `+` `-` 是干啥用的了…

Win10下记得先把中文输入法切换出去或者进入英文模式，不然按了没反应。