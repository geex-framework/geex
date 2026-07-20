export default {
  title: "模拟服务",
  home: {
    title: "模拟服务",
    description: "仅 SuperAdmin 可用的本地外部依赖模拟控制台.",
  },
  entries: {
    wechat: {
      title: "微信模拟档案",
      description: "管理用于微信授权模拟的 OpenId / 昵称档案.",
    },
    sms: {
      title: "短信收件箱",
      description: "查看与清理本地模拟短信及验证码.",
    },
    payments: {
      title: "支付模拟",
      description: "说明如何打开 createPayment 返回的模拟收银台链接.",
    },
  },
  wechat: {
    title: "微信模拟档案",
    openId: "OpenId",
    nickname: "昵称",
    unionId: "UnionId",
    enabled: "启用",
    create: "创建",
    loadFailed: "加载档案失败",
    createFailed: "创建档案失败",
  },
  sms: {
    title: "短信收件箱",
    phoneFilter: "按手机号筛选",
    phone: "手机号",
    params: "模板参数",
    captcha: "验证码",
    success: "成功",
    sentAt: "发送时间",
    loadFailed: "加载短信失败",
    clearFailed: "清空短信失败",
  },
  payments: {
    title: "支付模拟",
    description: "打开 createPayment 返回的 CodeUrl 即可进入模拟收银台, 例如 /mocking/payments/{token}.",
  },
};
