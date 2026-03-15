本仓库演示Unity在拥有大量动态游戏对象+对象交互情况下的性能优化演进过程。

* Unity: 2022.3.62f3
* CPU: 13th Gen Intel(R) Core(TM) i5-13490F | 2.50GHz
* RAM: 16.0 GB
* GPU: NVIDIA GeForce RTX 4060 | 8GB
* DISK: SSD
* System: Windows 11 专业版 x64 | 24H2 | 26100.4652

## 进程

* [ ] 10k 动态对象
* [ ] 100k 动态对象
* [ ] 1000k 动态对象

## 详细记录

### 10k规模 - 1 | FPS：3 (Editor)

| 行为               | 记录                                         | 截图                                                         |
| ------------------ | -------------------------------------------- | ------------------------------------------------------------ |
| 批量创建GameObject | 间隔：100ms<br />数量：100/次<br />总数：10k | <img src="./Files/Image/README/profile-10k-1.png" alt="image-20260315174312028" style="zoom:50%;" /> |
| 行为：每帧移动     | transform.Translate / Update                 | <img src="./Files/Image/README/image-20260315174800770.png" alt="image-20260315174800770" style="zoom:50%;" /> |
| 预制件：Enemy01    | Capsule / Rigidbody                          | <img src="./Files/Image/README/image-20260315174656347.png" alt="image-20260315174656347" style="zoom:50%;" /><br /><img src="./Files/Image/README/profile-10k-2.png" alt="image-20260315174517298" style="zoom:50%;" /> |
| 结果               | FPS：3                                       | ![](./Files/Image/README/image-20260315174959942.png)        |

### 10k规模 - 2 | FPS：25 (Editor)

**针对 3 FPS 的现状，执行以下步骤：**

1. 开启 Dynamic Batching：但在 Built-in 渲染管线下没有帮助。

2. 开启 GPU Instancing：将 Batches > 10000 降低到 Batches < 100 ，但当前情况下FPS没有明显提升。
3. 分析 Profiler：确认瓶颈来源是物理系统（Rigidbody）。
4. **★ 移除 Enemy Prefab 上的 Rigidbody：平均FPS提升到 25 （避开PhysX transform -> collider 反向同步）。**



<img src="./Files/Image/README/image-20260315184321667.png" alt="image-20260315184321667" style="zoom:50%;" />

<center>Profiler - 性能瓶颈为Rigidbody</center>



| 行为                        | 记录                                                         | 截图                                                         |
| --------------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| 批量创建GameObject          | 间隔：100ms<br />数量：100/次<br />总数：10k                 | <img src="./Files/Image/README/profile-10k-1.png" alt="image-20260315174312028" style="zoom:50%;" /> |
| 行为：每帧移动              | transform.Translate / Update                                 | <img src="./Files/Image/README/image-20260315174800770.png" alt="image-20260315174800770" style="zoom:50%;" /> |
| 预制件：Enemy01-NoRigidbody | Capsule<br />1. 移除Rigidbody<br />2. 使用自定义Material<br />3. 开启GPU Instancing | <img src="./Files/Image/README/image-20260315174656347.png" alt="image-20260315174656347" style="zoom:50%;" /><br /><img src="./Files/Image/README/image-20260315193409263.png" alt="image-20260315193409263" style="zoom:50%;" /> |
| 结果                        | FPS：25                                                      | ![image-20260315193315389](./Files/Image/README/image-20260315193315389.png) |

### 25k规模 - 1 | 编辑器 FPS：20 | 构建后 FPS：50

在当前逻辑下，提升GO数量到25k规模后，FPS稳定在10帧（编辑器运行）。

本轮优化完成后，编辑器内FPS稳定在 ~20 帧，构建后FPS稳定在 ~50 帧。



![image-20260315194233339](./Files/Image/README/image-20260315194233339.png)

<center>当前性能基线</center>



**进行性能分析，得到以下主要信息：**

1. 基础：PlayerLoop -> 占用 60ms 总计算时间。
2. Camera.Render -> 26ms | 渲染耗时。
3. Update.ScriptRunBehaviourUpdate -> 15ms | MonoBehaviour脚本耗时（Update），共计调用 25000 次/帧。
4. FixedUpdate.PhysicsFixedUpdate -> 15ms | 物理系统同步Collider和Transform操作耗时。



<img src="./Files/Image/README/image-20260315194459355.png" alt="image-20260315194459355" style="zoom:75%;" />



**本轮优化着重以下2点：**

1. 降低25k对象情况下Update（移动逻辑）运行压力。
2. 降低渲染压力。



**Update（移动逻辑）运行压力分析：**

1. 携带Collider的GameObject在每一次transform发生位移时，会触发PhysX的同步（FixedUpdate.PhysicsFixedUpdate）。

2. 调用每一个MonoBehaviour时的逻辑链开销和Unity管理MonoBehaviour开销。

3. 直接设置transform.position速度高于transform.Translate。



**渲染压力分析：**

1. Mesh Renderer 阴影计算耗时（Render.OpaqueGeometry）。



**步骤 - 1：优化物理开销、移动开销、Mono开销 | FPS变化：10 -> 13**

1. 移除Collider：避开PhysX同步开销，提升FPS 10 -> ~12。
   ![image-20260315203106314](./Files/Image/README/image-20260315203106314.png)
2. 集中管理移动逻辑：对Enemy去Mono化，降低 BehaviourUpdate 开销，~15ms -> ~7ms ，提升FPS ~12 -> ~13
   ![image-20260315203232046](./Files/Image/README/image-20260315203232046.png)
   1. 引入 EnemyManager 管理所有对象实例，移除MonoBehaviour。
   2. 通过 EnemyManager 直接移动所有Transform。（至此 ~11ms）
   3. 使用直接设置坐标 `transform.localPosition += movement` 替代 `transform.Translate` ，加速。（至此 ~7ms）



![image-20260315203808496](./Files/Image/README/image-20260315203808496.png)

<center>优化后帧率稳定到 ~13 帧</center>



**步骤 - 2：优化渲染开销**

1. 移除 Mesh Renderer 阴影渲染：降低 Render.OpaqueGeometry 开销 ~17ms -> ~5ms ，FPS变化 ~13 -> ~19 （编辑器内）。
   ![image-20260315211448328](./Files/Image/README/image-20260315211448328.png)



<img src="./Files/Image/README/image-20260315211731588.png" alt="image-20260315211731588" style="zoom:50%;" />

<center>优化后帧率在构建后FPS稳定到 ~50 帧</center>



![image-20260315211903850](./Files/Image/README/image-20260315211903850.png)

<center>优化后帧率在编辑器内FPS稳定到 ~20 帧</center>



### 100k规模 - 基线 | 编辑器 FPS：4 | 构建后 FPS：10



![image-20260315214920139](./Files/Image/README/image-20260315214920139.png)

<center>编辑器FPS：4</center>



<img src="./Files/Image/README/image-20260315214844589.png" alt="image-20260315214844589" style="zoom:50%;" />

<center>构建后FPS：10</center>

