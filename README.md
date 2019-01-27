# 网易云音乐歌词获取

## 一、使用方法

### 1.1 起步

1. 下载项目
2. 打开 `Software` 目录，`Green Version` 版免安装，直接运行即可；`Install Version` 版需要安装。

### 1.2 基本使用

1. 访问[网易云音乐](https://music.163.com)官网

2. 选择要获取的歌曲，进入播放页

3. 复制播放页 Url 中的歌曲 ID
    ```
    Exmaple:
    歌曲 That Girl
    播放页 Url: https://music.163.com/#/song?id=440208476
    ID: 440208476
    ```

4. 运行应用程序，将ID复制到输入框中，点击搜索。

5. 选择输出文件名，点击保存按钮。

6. 在弹出框中选择保存路径、保存类型。

![](https://www.imga.cc/imgs/2019/01/26bf7921de50d28b.png)

### 1.3 输出文件名

支持以下三种文件名格式：

1. 歌曲名 - 歌手
2. 歌手 - 歌曲名
3. 歌曲名

### 1.4 双语歌词

对于存在译文歌词的歌曲，支持以下五种模式，仍然以歌曲 440208476 为例：

#### 1.4.1 不显示译文

```
[by:刺客辣条之法与鲨]
[00:00.00] 作曲 : Olly Murs/Ina Wroldsen/Clarence Coffee Jr./Steve Robson
[00:00.10] 作词 : Olly Murs/Ina Wroldsen/Clarence Coffee Jr./Steve Robson
[00:00.30]There's a girl but I let her get away
[00:05.40]It's all my fault cause pride got in the way
[00:11.40]And I'd be lying if I said I was ok
[00:16.00]About that girl the one I let get away
...
```

#### 1.4.2 仅显示译文

```
[by:Nighingale_In_Nirvana]
[00:00.30]曾经心爱的女孩 我却让她擦肩而过
[00:05.40]自尊心作祟 一切都是我的错
[00:11.40]若说无事 其实只是谎言未戳破
[00:16.00]那个女孩 我们曾擦肩而过
...
```

#### 1.4.3 优先原文

```
[by:刺客辣条之法与鲨]
[by:Nighingale_In_Nirvana]
[00:00.00] 作曲 : Olly Murs/Ina Wroldsen/Clarence Coffee Jr./Steve Robson
[00:00.10] 作词 : Olly Murs/Ina Wroldsen/Clarence Coffee Jr./Steve Robson
[00:00.30]There's a girl but I let her get away
[00:00.30]曾经心爱的女孩 我却让她擦肩而过
[00:05.40]It's all my fault cause pride got in the way
[00:05.40]自尊心作祟 一切都是我的错
...
```

#### 1.4.5 优先译文

```
[by:刺客辣条之法与鲨]
[by:Nighingale_In_Nirvana]
[00:00.00] 作曲 : Olly Murs/Ina Wroldsen/Clarence Coffee Jr./Steve Robson
[00:00.10] 作词 : Olly Murs/Ina Wroldsen/Clarence Coffee Jr./Steve Robson
[00:00.30]曾经心爱的女孩 我却让她擦肩而过
[00:00.30]There's a girl but I let her get away
[00:05.40]自尊心作祟 一切都是我的错
[00:05.40]It's all my fault cause pride got in the way
...
```

#### 1.4.6 合并显示

需要指定 `合并显示分隔符`，以使用空格分割为例：

```
[by:刺客辣条之法与鲨]
[by:Nighingale_In_Nirvana]
[00:00.00] 作曲 : Olly Murs/Ina Wroldsen/Clarence Coffee Jr./Steve Robson
[00:00.10] 作词 : Olly Murs/Ina Wroldsen/Clarence Coffee Jr./Steve Robson
[00:00.30]There's a girl but I let her get away 曾经心爱的女孩 我却让她擦肩而过
[00:05.40]It's all my fault cause pride got in the way 自尊心作祟 一切都是我的错
...
```

### 1.5 强制两位

将时间戳中的三位小数调整为两位小数，例如将：

```
[00:26.490]一定还可以
```

调整为：

```
[00:26.49]一定还可以
```

### 1.6 批量保存

将多个歌曲 ID，使用英文下的逗号（,）分割，点击批量保存后。**只需要选择路径和保存类型即可，无需修改文件名。**

## 二、更新日志

#### 2019.1.28

- 移除强制排序功能，增加自定义双语歌词的输出格式

- 增加批量保存功能

#### 2017.10.4

- 对于有特殊需求的人，增加强制排序功能。如不开启，默认只对不包含非空时间戳的歌词排序

#### 2017.10.3

- 支持时间戳强制保留两位小数（测试），如无特殊需求无需开启

- 绿色版运行需要`Newtonsoft.Json.dll`的支持

- 重构代码

#### 2017.8.19

修复因为歌曲id错误导致的程序异常

#### 2017.7.27

现在保存歌词可以选择命名格式，为以下三种：

(1)歌曲名 - 歌手

(2)歌手 - 歌曲名

(3)歌曲名

默认为第一种

#### 2017.5.8

- 对中英文歌词结果排序显示

- 输入id号后显示歌词的同时能够显示歌曲名和歌手信息

- 保存文件时默认保存文件名为“歌曲名 - 歌手”的格式

#### 2017.3.11

- 为歌词增加了翻译歌词
