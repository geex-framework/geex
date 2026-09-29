# Enumeration API

Geex 的 `Enumeration<TEnum>` 将字符串 Value 与可读 Name 关联, 支持静态成员发现和未知值动态创建. 查找与定义入口在同一继承家族中按 Value 复用实例. 附加属性能够从 Name/Value 计算时由构造函数初始化; 需要外部数据的类型可使用 `Enumeration<TEnum, TDetails>` 显式接收属性定义.

## 普通枚举与动态构造

```csharp
public sealed class TaskState : Enumeration<TaskState>
{
    public static TaskState Pending { get; } = FromValue("pending");
}

var first = TaskState.FromValue("queued");
var second = TaskState.FromValue("queued");
```

`first` 与 `second` 是同一实例, Name/Value 均为 `"queued"`. 需要不同名称时, 使用 `FromNameAndValue(name, value)`. `FromExistedValue`/`FromExistedName` 仅查找已有成员, 不动态创建. `ToString()` 返回 Name, 隐式字符串转换返回 Value.

未知值的构造顺序:

1. 使用请求具体类型自身声明的精确 `(string name, string value)` 构造函数, 支持 public/internal/protected/private.
2. 没有二参构造时, `Enumeration<TRoot>` 派生类型可通过 public 无参构造创建, 再由框架设置 Name/Value.
3. 都不具备时抛出异常. 不推测单字符串构造/可选参数/其他业务参数, 不绕过构造函数创建未初始化对象.

二参构造必须保留传入的 Name/Value. 构造异常不会触发另一路回退, 无效实例不进入缓存. 无参构造执行时 Name/Value 尚未设置, 依赖 Value 的初始化应放在二参构造或计算属性中.

独立实现 `IEnumeration` 的类型可以使用 `Enumeration<TConcrete>.FromValue` 等入口, 但必须提供精确二参构造并自行初始化 Name/Value. 抽象类型和接口只能复用已知兼容实例, 不能凭空创建未知具体类型.

## 可选的外部属性定义

```csharp
public sealed record PolicyDetails(string? Description, int? Priority);

public sealed class PolicyKind : Enumeration<PolicyKind, PolicyDetails>
{
    private PolicyKind(string name, string value)
        : base(name, value, new PolicyDetails(null, null), isComplete: false)
    {
    }

    public int? Priority => Definition.Details.Priority;
}

var pending = PolicyKind.FromValue("regional");
var before = pending.Definition;
var complete = PolicyKind.Define(
    "regional", "regional", new PolicyDetails("Regional policy", 5));
var after = pending.Definition;
```

`pending` 与 `complete` 保持同一引用. `before.IsComplete` 为 false, `after.IsComplete` 为 true. 框架一次性替换快照, 已读取的 `before` 保持原状态. 示例中的 null 是该类型明确提供的未知状态; 框架不根据 null/0 或属性数量推断是否完整.

可选基类的公共契约:

| 入口 | 行为 |
| --- | --- |
| `Enumeration<TEnum, TDetails>` | 约束 `TEnum : class, IEnumeration`; `TDetails` 可以是引用类型或值类型, 属性定义不接受 null |
| protected `Enumeration(TDetails unknownDetails)` | 初始化该类型明确提供的未知属性, 配合派生类型 public 无参构造使用 |
| protected `Enumeration(string name, string value, TDetails details, bool isComplete)` | 同时初始化身份/属性快照与完整状态 |
| `Definition` | 返回不可变 `EnumerationDefinition<TDetails>` 快照, 包含 `Details` 和 `IsComplete` |
| `Define(name, value, details)` | 创建或补全该家族根类型的完整定义 |
| `Define<TChild>(name, value, details)` | 按指定具体子类型创建或补全, 仍使用同一家族缓存 |

未知属性由具体类型提供, 未完整定义的动态实例通过日志记录 Warning. 普通 `Enumeration<TEnum>` 无需声明未知状态. 不具备动态构造约定的业务类型保持其构造要求, 框架不会自动为 `SettingScopeEnumeration`/`SettingDefinition`/`ApprovalFlowAssociatedEntityType` 等类型填充业务属性.

`Define` 在无实例时创建完整实例; 已有基础实例时补全同一实例, 保留原 Name/Value. 已完整定义时, 使用 `EqualityComparer<TDetails>.Default` 比较属性: 相同定义可重复提交, 不同定义拒绝且保留原快照. 名称冲突/类型不兼容/定义失败不得改变已有实例. 从基础状态到完整状态的发布是原子的, 无公开 setter 或任意 initializer 回调.

`TDetails` 应使用不可变值对象或 record. 快照容器不可变不意味着框架会深拷贝属性对象; 不应在发布后修改其中的集合/对象. 默认相等比较使用该类型自身语义, 数组等引用类型不会自动按元素比较. 一次读取多个相关属性时先保存 `var definition = instance.Definition`, 再从该快照读取, 避免跨越一次补全读取不同版本.

## 继承家族/静态成员与别名

家族由实际继承的 `Enumeration<TRoot>` 确定. `Root.FromValue<Child>(value)` 与反射/序列化的 Child 入口共享该家族缓存. 已存在兼容的派生实例可以作为根类型复用; 不兼容类型不能占用同一个 Value 创建第二个实例. 同 Value/不同 Name 保留既有名称并警告, 同 Name/不同 Value 拒绝.

静态成员可通过 public static readonly 字段或 public 静态属性声明. 发现过程先登记已赋值的 readonly 字段/自动属性 backing field. 已知成员查找可直接复用, 不强制执行计算属性; `List` 或未命中的查找继续完整发现. 框架发起的初始化重入只读取已赋值存储. 计算属性仍需遵循 CLR 静态初始化顺序, 不应依赖正在初始化且尚未赋值的成员; 不在静态构造中对这种计算属性执行列表或未知值查询. 程序集重新发现不会清空动态实例或别名.

`FromValue(value, aliases)` 将别名关联到规范实例. 一批别名全部通过校验后才写入, 冲突不会留下部分别名绑定; 此前成功解析的规范实例仍然保留. 直接 `new` 不提供缓存复用保证, 需要身份稳定时使用查找/定义入口.

权限类型的单参构造遵循模块短值约定, 二参构造接收完整 Name/Value 并直接传给父类, 不再次附加模块前缀. 动态构造保留完整 Value, 由权限构造逻辑初始化 `Mod`/`Obj`/`Field`.

## JSON/BSON 与 GraphQL

JSON 将枚举写为 Value 字符串. 读取时先查已有 Value/值别名与 Name: 仅一侧命中或两侧指向同一实例时复用, 两侧指向不同实例时抛出明确的歧义异常, 均未命中时按声明具体类型动态创建. 旧 JSON 中的 Name 只有仍存在名称映射时才能恢复对应 Value.

BSON 保持 Value 字符串表示. JSON/BSON 都不保存 `Definition` 属性快照, 外部定义必须由应用配置或其他持久化来源重新登记. 字符串记录本身不能还原外部业务属性.

GraphQL ENUM 的可用值仍由 schema 声明范围决定. CLR 中创建未知动态值不会自动扩展已经生成的 GraphQL schema.

验证入口和范围见 [Enumeration 动态枚举专项](testing/scopes/enumeration-dynamic.md) 与 [测试入口](testing/README.md). 文档描述接口契约, 实际验证状态以各次冻结证据包为准.
