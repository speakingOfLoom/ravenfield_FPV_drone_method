Ravenfield爆改穿越机模拟器插件

【功能说明】
- 能让你自由选择游戏中任意直升机类载具(底层MonoBehaviour为Helicopter类的载具)，将其操控逻辑及其气动改为穿越机。
//不要使用键鼠，玩起来会非常难受。可以使用普通手柄或者使用专业航模手柄操控，玩穿越机的都知道为什么....  
- 目前，可以自定义的参数主要包括：
rate曲线(采用actual模型)
推重比
螺旋桨螺距
油门加减速响应
空气阻力
平飞升力加成
近地升力加成
//参数文件路径位于Ravenfield\BepInEx\config\local.ravenfield.heliagilitycap.cfg，第一次启动会自动生成


【环境要求】
- 游戏：Ravenfield（Steam 版，Windows。IOS没试过）
- 前置：
BepInEx，下载：github.com/BepInEx/BepInEx/releases


【安装】
1. 安装 BepInEx：下载后解压到 Ravenfield 游戏根目录（Ravenfield.exe 所在文件夹，通常是...\steamapps\common\Ravenfield），先启动一次游戏，让它生成 BepInEx\ 目录结构。
2. 下载此项目中的HeliAgilityCap.dll，将其放入 Ravenfield\BepInEx\plugins\。
3. 启动游戏后 —— 如果正常的话，配置文件会自动生成在 Ravenfield\BepInEx\config\local.ravenfield.heliagilitycap.cfg。


【使用步骤】
1. 进入游戏后，在OPTION-INPUT界面，勾选顶部的Allow Joystick Binds，调整Joystick Deadzone中央死区，正常的航模手柄直接拉到0.01就行
2. 下拉到直升机相关键位设置(Heli Throttle Up/Down)，绑定各个通道。如果绑定失败的话可能是此航模遥控不支持，可以去下个输入转换器，比如x360ce(下载地址： www.x360ce.com )，把输入映射成xbox手柄(这玩意也能拿去连其它游戏)
3. 去游戏里找个你选择爆改成穿越机的直升机上去开(配置文件中可以自己定义哪些启用穿越机逻辑，把此载具的名字/部分名字输入Names以及RateNames字段即可)
//默认载具名字包含"FPV"的直升机类载具启用，推荐创意工坊中的FPV kamikaze drones [PG-7v, OG-7v](https://steamcommunity.com/sharedfiles/filedetails/?id=3701263254)//注意，是载具类的那个。这个装了能直接用
4. 飞一下，如果不舒服可以去配置文件里调那些参数


//有个小问题，人机开被你爆改过的直升机貌似会鬼畜炸机。。。。如果有空我会把它改了的
