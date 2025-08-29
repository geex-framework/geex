# 乐观锁并发控制

## 概述

DbContext 现在支持基于 `ModifiedOn` 字段的乐观锁并发控制。当检测到数据库中的实体 `ModifiedOn` 值比缓存值更新时，系统会自动进行冲突检测和处理。

## 工作原理

### 乐观锁检测

系统在以下情况下会进行乐观锁检查：
- 调用 `UpdateDbDataCache` 方法更新缓存时
- 通过 `CachedDbContextQueryProvider` 或 `CachedDbContextQueryable` 查询数据时

### 冲突处理逻辑

当检测到 `ModifiedOn` 冲突时，系统会按以下逻辑处理：

#### 1. 本地无修改的情况
如果本地值没有发生任何修改（MemoryDataCache 值与 DbDataCache 值相同）：
- 直接更新缓存值为数据库最新值
- 记录警告日志
- 不抛出异常

#### 2. 本地有修改的情况
如果本地值发生了修改：
- 比较本地修改的字段与数据库修改的字段
- 如果有字段冲突，抛出 `OptimisticConcurrencyException`
- 如果没有字段冲突，记录信息日志，允许继续

## 异常处理

### OptimisticConcurrencyException

此异常继承自 `MongoWriteException`，当检测到字段冲突时抛出，包含：
- `EntityType`: 冲突的实体类型
- `EntityId`: 实体ID
- `ConflictingFields`: 冲突字段的详细信息列表

### FieldConflictInfo

每个冲突字段包含：
- `FieldName`: 字段名
- `BaseValue`: 缓存中的原始值
- `OurValue`: 本地的当前值
- `TheirValue`: 数据库中的最新值

### 示例代码

```csharp
try
{
    await dbContext.SaveChanges();
}
catch (OptimisticConcurrencyException ex)
{
    logger.LogError("乐观锁冲突: {Message}", ex.Message);
    
    foreach (var conflict in ex.ConflictingFields)
    {
        logger.LogError("字段冲突: {ConflictInfo}", conflict);
    }
    
    // 处理冲突...
}
```

## 日志记录

系统记录以下日志：

### Debug 级别
- ModifiedOn 变化检测

### Warning 级别  
- 数据库更新但本地无变化

### Information 级别
- 并发修改但无字段冲突

### Error 级别
- 字段冲突检测

## 注意事项

1. 乐观锁检查仅基于 `ModifiedOn` 字段进行
2. 只有启用了 `EntityTrackingEnabled` 的 DbContext 才会进行冲突检测
3. 字段冲突检测基于 BSON 级别的差异比较
4. 系统会自动处理无冲突的并发修改情况
5. 对于有冲突的情况，应用程序需要实现适当的冲突解决策略

## 性能考虑

- 乐观锁检查仅在缓存更新时进行，不会影响正常的查询性能
- 字段差异比较使用了高效的 BSON 比较算法
- 冲突检测是惰性的，只有在实际发生冲突时才会进行详细比较
