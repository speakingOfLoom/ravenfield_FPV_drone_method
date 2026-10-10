English | [中文 ↓](#ravenfield-爆改穿越机模拟器插件)

---

# Ravenfield FPV Drone Simulator Plugin

## Features

- Lets you take any helicopter-type vehicle in the game (any vehicle whose underlying MonoBehaviour is the `Helicopter` class) and replace its control logic and aerodynamics with FPV drone behaviour.

  // Do NOT use keyboard and mouse — it will feel awful(When a real FPV drone hovers, the throttle is often not at 50%. If you control the throttle with a keyboard, you have to keep tapping W or S, which makes it very difficult to maintain a stable hover). Use a regular gamepad, or better, a proper RC transmitter. Anyone who flies FPV knows why...

- Currently configurable parameters include:
  - Rate curves (betaflight Actual model)
  - Attitude Change Response Speed (Simulated PID Response)
  - Thrust-to-weight ratio
  - Propeller pitch
  - Throttle response (spool-up/spool-down)
  - Air drag
  - Level-flight lift bonus
  - Ground-effect lift bonus

// The config file is at `Ravenfield\BepInEx\config\local.ravenfield.heliagilitycap.cfg` and is generated automatically on first launch. And....I am very sorry that the parameter comments in the original configuration file are not provided in English. I will attach the English version of the configuration file to this project (named local.ravenfield.heliagilitycap.cfg).

## Requirements

- Game: Ravenfield (Steam version, Windows. Untested on iOS.)
- Prerequisite: BepInEx — download: [github.com/BepInEx/BepInEx/releases](https://github.com/BepInEx/BepInEx/releases)


## Installation

1. Download and extract the archive, then copy the BepInEx folder, winhttp.dll, and doorstop_config.ini into your Ravenfield root directory. (the folder containing Ravenfield.exe, usually `...\steamapps\common\Ravenfield`). Launch the game once so it generates the `BepInEx\` folder structure.
2. Download `HeliAgilityCap.dll` from this project and place it in `Ravenfield\BepInEx\plugins\`.
3. Launch the game — if all went well, the config file will be generated at `Ravenfield\BepInEx\config\local.ravenfield.heliagilitycap.cfg`.

## Usage

1. In-game, go to OPTION → INPUT, tick **Allow Joystick Binds** at the top, and adjust the **Joystick Deadzone**. For a normal RC transmitter, just set it to 0.01.
2. Scroll down to the helicopter key bindings (**Heli Throttle Up/Down**) and bind each channel. If binding fails, your transmitter may not be supported — you can use an input wrapper such as x360ce (download: [www.x360ce.com](https://www.x360ce.com)) to map your inputs to an Xbox controller. (It works for other games too.)
3. Find a helicopter in-game that you want to convert into an FPV drone and fly it. (You can choose which vehicles use the FPV logic in the config file — just put the vehicle's full name, or part of it, into the `RateNames` and `ThrustRpmNames` fields.)

   // By default, any helicopter-type vehicle whose name contains "FPV" uses the FPV logic. Recommended: the FPV kamikaze drones [PG-7v, OG-7v](https://steamcommunity.com/sharedfiles/filedetails/?id=3701263254)

   // Note that you want the *vehicle* version. Install that and you're good to go.

4. Go fly. If it doesn't feel right, tune the parameters in the config file.
//The default rate curves, or roll sensitivity parameters, are designed for professional RC transmitters used in model aircraft. For players using gamepads with lower precision, these settings may be difficult to adapt to. You can try modifying the Roll/Yaw/Pitch Rates in the configuration file to 270, 270, 0.1. If you find it hard to control altitude or hover steadily, you can reduce the RotorForce value.
---

[↑ English](#ravenfield-fpv-drone-simulator-plugin)

# Ravenfield 爆改穿越机模拟器插件

## 功能说明

- 能让你自由选择游戏中任意直升机类载具(底层MonoBehaviour为Helicopter类的载具)，将其操控逻辑及其气动改为穿越机。

  //不要使用键鼠，玩起来会非常难受（真实穿越机悬停时油门很多时候并不在50%，你用键盘控制穿越机油门的话要一直点按w或s，这非常难控制悬停）。可以使用普通手柄或者使用专业航模手柄操控，玩穿越机的都知道为什么....

- 目前，可以自定义的参数主要包括：
  - rate曲线(采用actual模型)
  - 姿态改变响应速度(模拟PID响应)
  - 推重比
  - 螺旋桨螺距
  - 油门加减速响应
  - 空气阻力
  - 平飞升力加成
  - 近地升力加成

//参数文件路径位于 `Ravenfield\BepInEx\config\local.ravenfield.heliagilitycap.cfg`，第一次启动会自动生成

## 环境要求

- 游戏：Ravenfield（Steam 版，Windows。IOS没试过）
- 前置：BepInEx，下载：[github.com/BepInEx/BepInEx/releases](https://github.com/BepInEx/BepInEx/releases)

## 安装

1. 安装 BepInEx：下载后解压，把其中的BepInEx文件夹、winhttp.dll、doorstop_config.ini复制到 Ravenfield 游戏根目录（Ravenfield.exe 所在文件夹，通常是 `...\steamapps\common\Ravenfield`），先启动一次游戏，让它生成 `BepInEx\` 目录结构。
2. 下载此项目中的 `HeliAgilityCap.dll`，将其放入 `Ravenfield\BepInEx\plugins\`。
3. 启动游戏后 —— 如果正常的话，配置文件会自动生成在 `Ravenfield\BepInEx\config\local.ravenfield.heliagilitycap.cfg`。

## 使用步骤

1. 进入游戏后，在 OPTION-INPUT 界面，勾选顶部的 **Allow Joystick Binds**，调整 **Joystick Deadzone** 中央死区，正常的航模手柄直接拉到 0.01 就行
2. 下拉到直升机相关键位设置(**Heli Throttle Up/Down**)，绑定各个通道。如果绑定失败的话可能是此航模遥控不支持，可以去下个输入转换器，比如 x360ce(下载地址：[www.x360ce.com](https://www.x360ce.com))，把输入映射成 xbox 手柄(这玩意也能拿去连其它游戏)
3. 去游戏里找个你选择爆改成穿越机的直升机上去开(配置文件中可以自己定义哪些启用穿越机逻辑，把此载具的名字/部分名字输入 `RateNames` 以及 `ThrustRpmNames` 字段即可)

   //默认载具名字包含"FPV"的直升机类载具启用，推荐创意工坊中的 FPV kamikaze drones [PG-7v, OG-7v](https://steamcommunity.com/sharedfiles/filedetails/?id=3701263254)

   //注意，是载具类的那个。这个装了能直接用

4. 飞一下，如果不舒服可以去配置文件里调那些参数
//默认的Rate curves或者说翻滚灵敏度参数是针对专业航模手柄的，对于精度较低的游戏手柄玩家来说可能难以适应，可以尝试去配置文件中把Roll/Yaw/Pitch Rates改成270,270,0.1。如果觉得难以控制高度/悬停，可以把RotorForce这一项调低
---

//有个小问题，人机开被你爆改过的直升机貌似会鬼畜。。。。如果有空我会把它改了的