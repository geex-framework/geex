export default {
  title: "Mocking",
  home: {
    title: "Mocking",
    description: "SuperAdmin-only console for local external dependency simulation.",
  },
  entries: {
    wechat: {
      title: "WeChat profiles",
      description: "Manage OpenId / nickname profiles used by WeChat authorize mocks.",
    },
    sms: {
      title: "SMS inbox",
      description: "Inspect and clear locally simulated SMS messages and captchas.",
    },
    payments: {
      title: "Payments",
      description: "How to open the mock checkout URL returned by createPayment.",
    },
  },
  wechat: {
    title: "WeChat profiles",
    openId: "OpenId",
    nickname: "Nickname",
    unionId: "UnionId",
    enabled: "Enabled",
    create: "Create",
    loadFailed: "Failed to load profiles",
    createFailed: "Failed to create profile",
  },
  sms: {
    title: "SMS inbox",
    phoneFilter: "Filter by phone",
    phone: "Phone",
    params: "Params",
    captcha: "Captcha",
    success: "Success",
    sentAt: "Sent at",
    loadFailed: "Failed to load SMS messages",
    clearFailed: "Failed to clear SMS messages",
  },
  payments: {
    title: "Payments",
    description: "Open the CodeUrl returned by createPayment, for example /mocking/payments/{token}.",
  },
};
