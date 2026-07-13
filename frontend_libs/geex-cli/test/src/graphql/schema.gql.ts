export type Maybe<T> = T | null;
export type InputMaybe<T> = Maybe<T>;
export type Exact<T extends { [key: string]: unknown }> = { [K in keyof T]: T[K] };
export type MakeOptional<T, K extends keyof T> = Omit<T, K> & { [SubKey in K]?: Maybe<T[SubKey]> };
export type MakeMaybe<T, K extends keyof T> = Omit<T, K> & { [SubKey in K]: Maybe<T[SubKey]> };
export type MakeEmpty<T extends { [key: string]: unknown }, K extends keyof T> = { [_ in K]?: never };
export type Incremental<T> = T | { [P in keyof T]?: P extends ' $fragmentName' | '__typename' ? T[P] : never };
/** All built-in and custom scalars, mapped to their actual values */
export interface Scalars {
  ID: { input: string; output: string; }
  String: { input: string; output: string; }
  Boolean: { input: boolean; output: boolean; }
  Int: { input: number; output: number; }
  Float: { input: number; output: number; }
  Any: { input: any; output: any; }
  /** ^[1]([3-9])[0-9]{9}$ */
  ChinesePhoneNumber: { input: string; output: string; }
  /** The `DateTime` scalar represents an ISO-8601 compliant date time type. */
  DateTime: { input: Date; output: Date; }
  /** The `Long` scalar type represents non-fractional signed whole 64-bit numeric values. Long can represent values between -(2^63) and 2^63 - 1. */
  Long: { input: BigInt; output: BigInt; }
  /** mime type, e.g. application/json */
  MimeType: { input: string; output: string; }
  ObjectId: { input: string; output: string; }
  /** The `Upload` scalar type represents a file upload. */
  Upload: { input: any; output: any; }
}

export enum AppPermission {
  authorization_mutation_authorize = 'authorization_mutation_authorize',
  geexModGeex_mutation_editGeexEntityGeex = 'geexModGeex_mutation_editGeexEntityGeex',
  geexModGeex_query_geexEntityGeex = 'geexModGeex_query_geexEntityGeex',
  identity_mutation_createOrg = 'identity_mutation_createOrg',
  identity_mutation_createRole = 'identity_mutation_createRole',
  identity_mutation_createUser = 'identity_mutation_createUser',
  identity_mutation_editOrg = 'identity_mutation_editOrg',
  identity_mutation_editRole = 'identity_mutation_editRole',
  identity_mutation_editUser = 'identity_mutation_editUser',
  identity_query_roles = 'identity_query_roles',
  identity_query_users = 'identity_query_users',
  multiTenant_mutation_createTenant = 'multiTenant_mutation_createTenant',
  multiTenant_mutation_deleteTenant = 'multiTenant_mutation_deleteTenant',
  multiTenant_mutation_editTenant = 'multiTenant_mutation_editTenant',
  multiTenant_query_tenants = 'multiTenant_query_tenants',
  quicollab_mutation_addDeviceToSquad = 'quicollab_mutation_addDeviceToSquad',
  quicollab_mutation_addMember = 'quicollab_mutation_addMember',
  quicollab_mutation_authorizeDevice = 'quicollab_mutation_authorizeDevice',
  quicollab_mutation_batchAuthorizeDevices = 'quicollab_mutation_batchAuthorizeDevices',
  quicollab_mutation_batchRevokeDevices = 'quicollab_mutation_batchRevokeDevices',
  quicollab_mutation_createSquad = 'quicollab_mutation_createSquad',
  quicollab_mutation_deleteSquad = 'quicollab_mutation_deleteSquad',
  quicollab_mutation_editSquad = 'quicollab_mutation_editSquad',
  quicollab_mutation_removeDeviceFromSquad = 'quicollab_mutation_removeDeviceFromSquad',
  quicollab_mutation_removeMember = 'quicollab_mutation_removeMember',
  quicollab_mutation_removeMemberAdmin = 'quicollab_mutation_removeMemberAdmin',
  quicollab_mutation_revokeDevice = 'quicollab_mutation_revokeDevice',
  quicollab_mutation_setActiveDevice = 'quicollab_mutation_setActiveDevice',
  quicollab_mutation_setMemberAsAdmin = 'quicollab_mutation_setMemberAsAdmin',
  quicollab_mutation_updateDevice = 'quicollab_mutation_updateDevice',
  quicollab_mutation_updateSquadSettings = 'quicollab_mutation_updateSquadSettings',
  schematics_mutation_editTemplate = 'schematics_mutation_editTemplate',
  schematics_query_template = 'schematics_query_template',
  settings_mutation_editSetting = 'settings_mutation_editSetting'
}

export enum AppSettings {
  AppAppMenu = 'AppAppMenu',
  AppAppName = 'AppAppName',
  AppPermissions = 'AppPermissions'
}

export enum ApplyPolicy {
  AFTER_RESOLVER = 'AFTER_RESOLVER',
  BEFORE_RESOLVER = 'BEFORE_RESOLVER',
  VALIDATION = 'VALIDATION'
}

export interface ApprovalFlowFilterInput {
  activeIndex?: InputMaybe<IntOperationFilterInput>;
  activeNode?: InputMaybe<ApprovalFlowNodeFilterInput>;
  and?: InputMaybe<Array<ApprovalFlowFilterInput>>;
  /** 关联的实体对象 */
  associatedEntity?: InputMaybe<IApproveEntityFilterInput>;
  associatedEntityId?: InputMaybe<StringOperationFilterInput>;
  /** 关联的实体对象类型 */
  associatedEntityType?: InputMaybe<ClassEnumOperationFilterInputTypeOfAssociatedEntityTypeFilterInput>;
  canEdit?: InputMaybe<BooleanOperationFilterInput>;
  createdOn?: InputMaybe<DateTimeOperationFilterInput>;
  creatorUser?: InputMaybe<UserFilterInput>;
  creatorUserId?: InputMaybe<StringOperationFilterInput>;
  description?: InputMaybe<StringOperationFilterInput>;
  id?: InputMaybe<StringOperationFilterInput>;
  modifiedOn?: InputMaybe<DateTimeOperationFilterInput>;
  name?: InputMaybe<StringOperationFilterInput>;
  nodes?: InputMaybe<ListFilterInputTypeOfApprovalFlowNodeFilterInput>;
  or?: InputMaybe<Array<ApprovalFlowFilterInput>>;
  orgCode?: InputMaybe<StringOperationFilterInput>;
  stakeholders?: InputMaybe<ListFilterInputTypeOfApprovalFlowUserRefFilterInput>;
  status?: InputMaybe<ApprovalFlowStatusOperationFilterInput>;
  templateId?: InputMaybe<StringOperationFilterInput>;
  /** 租户编码, 为null时为宿主数据 */
  tenantCode?: InputMaybe<StringOperationFilterInput>;
}

export interface ApprovalFlowNodeDataInput {
  approvalFlowId?: InputMaybe<Scalars['String']['input']>;
  auditRole?: InputMaybe<Scalars['String']['input']>;
  auditUserId?: InputMaybe<Scalars['String']['input']>;
  carbonCopyUserIds?: InputMaybe<Array<Scalars['String']['input']>>;
  description?: InputMaybe<Scalars['String']['input']>;
  id?: InputMaybe<Scalars['String']['input']>;
  index?: InputMaybe<Scalars['Int']['input']>;
  isFromTemplate?: InputMaybe<Scalars['Boolean']['input']>;
  name?: InputMaybe<Scalars['String']['input']>;
}

export interface ApprovalFlowNodeFilterInput {
  and?: InputMaybe<Array<ApprovalFlowNodeFilterInput>>;
  approvalComment?: InputMaybe<StringOperationFilterInput>;
  approvalFlow?: InputMaybe<ApprovalFlowFilterInput>;
  approvalFlowId?: InputMaybe<StringOperationFilterInput>;
  approvalTime?: InputMaybe<DateTimeOperationFilterInput>;
  auditRole?: InputMaybe<StringOperationFilterInput>;
  auditUser?: InputMaybe<IUserFilterInput>;
  auditUserId?: InputMaybe<StringOperationFilterInput>;
  carbonCopyUserIds?: InputMaybe<ListStringOperationFilterInput>;
  chatLogs?: InputMaybe<ListFilterInputTypeOfApprovalFlowNodeLogFilterInput>;
  consultUser?: InputMaybe<IUserFilterInput>;
  consultUserId?: InputMaybe<StringOperationFilterInput>;
  createdOn?: InputMaybe<DateTimeOperationFilterInput>;
  description?: InputMaybe<StringOperationFilterInput>;
  id?: InputMaybe<StringOperationFilterInput>;
  index?: InputMaybe<IntOperationFilterInput>;
  isFromTemplate?: InputMaybe<BooleanOperationFilterInput>;
  modifiedOn?: InputMaybe<DateTimeOperationFilterInput>;
  name?: InputMaybe<StringOperationFilterInput>;
  nextNode?: InputMaybe<ApprovalFlowNodeFilterInput>;
  nodeStatus?: InputMaybe<ApprovalFlowNodeStatusOperationFilterInput>;
  or?: InputMaybe<Array<ApprovalFlowNodeFilterInput>>;
  previousNode?: InputMaybe<ApprovalFlowNodeFilterInput>;
}

export interface ApprovalFlowNodeLogFilterInput {
  and?: InputMaybe<Array<ApprovalFlowNodeLogFilterInput>>;
  approvalFlowNode?: InputMaybe<ApprovalFlowNodeFilterInput>;
  approvalFlowNodeId?: InputMaybe<StringOperationFilterInput>;
  createdOn?: InputMaybe<DateTimeOperationFilterInput>;
  creationTime?: InputMaybe<DateTimeOperationFilterInput>;
  from?: InputMaybe<UserFilterInput>;
  fromUserId?: InputMaybe<StringOperationFilterInput>;
  id?: InputMaybe<StringOperationFilterInput>;
  logType?: InputMaybe<ApprovalFlowNodeLogTypeOperationFilterInput>;
  message?: InputMaybe<StringOperationFilterInput>;
  modifiedOn?: InputMaybe<DateTimeOperationFilterInput>;
  or?: InputMaybe<Array<ApprovalFlowNodeLogFilterInput>>;
  to?: InputMaybe<UserFilterInput>;
  toUserId?: InputMaybe<StringOperationFilterInput>;
}

export enum ApprovalFlowNodeLogType {
  APPROVE = 'APPROVE',
  ATTACH_FILE = 'ATTACH_FILE',
  CARBON_COPY = 'CARBON_COPY',
  CHAT = 'CHAT',
  CONSULT = 'CONSULT',
  CONSULT_CHAT = 'CONSULT_CHAT',
  EDIT = 'EDIT',
  REJECT = 'REJECT',
  TRANSFER = 'TRANSFER',
  VIEW = 'VIEW',
  WITHDRAW = 'WITHDRAW'
}

export interface ApprovalFlowNodeLogTypeOperationFilterInput {
  eq?: InputMaybe<ApprovalFlowNodeLogType>;
  in?: InputMaybe<Array<ApprovalFlowNodeLogType>>;
  neq?: InputMaybe<ApprovalFlowNodeLogType>;
  nin?: InputMaybe<Array<ApprovalFlowNodeLogType>>;
}

export interface ApprovalFlowNodeSortInput {
  approvalComment?: InputMaybe<SortEnumType>;
  approvalFlow?: InputMaybe<ApprovalFlowSortInput>;
  approvalFlowId?: InputMaybe<SortEnumType>;
  approvalTime?: InputMaybe<SortEnumType>;
  auditRole?: InputMaybe<SortEnumType>;
  auditUser?: InputMaybe<IUserSortInput>;
  auditUserId?: InputMaybe<SortEnumType>;
  consultUser?: InputMaybe<IUserSortInput>;
  consultUserId?: InputMaybe<SortEnumType>;
  createdOn?: InputMaybe<SortEnumType>;
  description?: InputMaybe<SortEnumType>;
  id?: InputMaybe<SortEnumType>;
  index?: InputMaybe<SortEnumType>;
  isFromTemplate?: InputMaybe<SortEnumType>;
  modifiedOn?: InputMaybe<SortEnumType>;
  name?: InputMaybe<SortEnumType>;
  nextNode?: InputMaybe<ApprovalFlowNodeSortInput>;
  nodeStatus?: InputMaybe<SortEnumType>;
  previousNode?: InputMaybe<ApprovalFlowNodeSortInput>;
}

export interface ApprovalFlowNodeStatusFlagsInput {
  isApproved?: InputMaybe<Scalars['Boolean']['input']>;
  isConsulting?: InputMaybe<Scalars['Boolean']['input']>;
  isCreated?: InputMaybe<Scalars['Boolean']['input']>;
  isRejected?: InputMaybe<Scalars['Boolean']['input']>;
  isStarted?: InputMaybe<Scalars['Boolean']['input']>;
  isTransferred?: InputMaybe<Scalars['Boolean']['input']>;
  isViewed?: InputMaybe<Scalars['Boolean']['input']>;
}

export interface ApprovalFlowNodeStatusOperationFilterInput {
  eq?: InputMaybe<ApprovalFlowNodeStatusFlagsInput>;
  in?: InputMaybe<Array<ApprovalFlowNodeStatusFlagsInput>>;
  neq?: InputMaybe<ApprovalFlowNodeStatusFlagsInput>;
  nin?: InputMaybe<Array<ApprovalFlowNodeStatusFlagsInput>>;
}

export interface ApprovalFlowNodeTemplateDataInput {
  associatedEntityType?: InputMaybe<AssociatedEntityType>;
  auditRole: Scalars['String']['input'];
  carbonCopyUserIds?: InputMaybe<Array<Scalars['String']['input']>>;
  id: Scalars['String']['input'];
  index?: InputMaybe<Scalars['Int']['input']>;
  name: Scalars['String']['input'];
}

export interface ApprovalFlowNodeTemplateFilterInput {
  and?: InputMaybe<Array<ApprovalFlowNodeTemplateFilterInput>>;
  auditRole?: InputMaybe<StringOperationFilterInput>;
  carbonCopyUserIds?: InputMaybe<ListStringOperationFilterInput>;
  id?: InputMaybe<StringOperationFilterInput>;
  index?: InputMaybe<IntOperationFilterInput>;
  name?: InputMaybe<StringOperationFilterInput>;
  or?: InputMaybe<Array<ApprovalFlowNodeTemplateFilterInput>>;
}

export enum ApprovalFlowOwnershipType {
  CARBON_COPY = 'CARBON_COPY',
  CONSULT = 'CONSULT',
  CREATE = 'CREATE',
  PARTICIPATE = 'PARTICIPATE',
  SUBSCRIBE = 'SUBSCRIBE'
}

export interface ApprovalFlowOwnershipTypeOperationFilterInput {
  eq?: InputMaybe<ApprovalFlowOwnershipType>;
  in?: InputMaybe<Array<ApprovalFlowOwnershipType>>;
  neq?: InputMaybe<ApprovalFlowOwnershipType>;
  nin?: InputMaybe<Array<ApprovalFlowOwnershipType>>;
}

export interface ApprovalFlowSortInput {
  activeIndex?: InputMaybe<SortEnumType>;
  activeNode?: InputMaybe<ApprovalFlowNodeSortInput>;
  /** 关联的实体对象 */
  associatedEntity?: InputMaybe<IApproveEntitySortInput>;
  associatedEntityId?: InputMaybe<SortEnumType>;
  /** 关联的实体对象类型 */
  associatedEntityType?: InputMaybe<SortEnumType>;
  canEdit?: InputMaybe<SortEnumType>;
  createdOn?: InputMaybe<SortEnumType>;
  creatorUser?: InputMaybe<UserSortInput>;
  creatorUserId?: InputMaybe<SortEnumType>;
  description?: InputMaybe<SortEnumType>;
  id?: InputMaybe<SortEnumType>;
  modifiedOn?: InputMaybe<SortEnumType>;
  name?: InputMaybe<SortEnumType>;
  orgCode?: InputMaybe<SortEnumType>;
  status?: InputMaybe<SortEnumType>;
  templateId?: InputMaybe<SortEnumType>;
  /** 租户编码, 为null时为宿主数据 */
  tenantCode?: InputMaybe<SortEnumType>;
}

export enum ApprovalFlowStatus {
  CANCELED = 'CANCELED',
  FINISHED = 'FINISHED',
  PROCESSING = 'PROCESSING'
}

export interface ApprovalFlowStatusOperationFilterInput {
  eq?: InputMaybe<ApprovalFlowStatus>;
  in?: InputMaybe<Array<ApprovalFlowStatus>>;
  neq?: InputMaybe<ApprovalFlowStatus>;
  nin?: InputMaybe<Array<ApprovalFlowStatus>>;
}

export interface ApprovalFlowTemplateFilterInput {
  and?: InputMaybe<Array<ApprovalFlowTemplateFilterInput>>;
  createdOn?: InputMaybe<DateTimeOperationFilterInput>;
  creatorUser?: InputMaybe<IUserFilterInput>;
  creatorUserId?: InputMaybe<StringOperationFilterInput>;
  description?: InputMaybe<StringOperationFilterInput>;
  id?: InputMaybe<StringOperationFilterInput>;
  modifiedOn?: InputMaybe<DateTimeOperationFilterInput>;
  name?: InputMaybe<StringOperationFilterInput>;
  nodes?: InputMaybe<ListFilterInputTypeOfApprovalFlowNodeTemplateFilterInput>;
  or?: InputMaybe<Array<ApprovalFlowTemplateFilterInput>>;
  orgCode?: InputMaybe<StringOperationFilterInput>;
}

export interface ApprovalFlowTemplateSortInput {
  createdOn?: InputMaybe<SortEnumType>;
  creatorUser?: InputMaybe<IUserSortInput>;
  creatorUserId?: InputMaybe<SortEnumType>;
  description?: InputMaybe<SortEnumType>;
  id?: InputMaybe<SortEnumType>;
  modifiedOn?: InputMaybe<SortEnumType>;
  name?: InputMaybe<SortEnumType>;
  orgCode?: InputMaybe<SortEnumType>;
}

export interface ApprovalFlowUserRefFilterInput {
  and?: InputMaybe<Array<ApprovalFlowUserRefFilterInput>>;
  approvalFlowId?: InputMaybe<StringOperationFilterInput>;
  createdOn?: InputMaybe<DateTimeOperationFilterInput>;
  id?: InputMaybe<StringOperationFilterInput>;
  modifiedOn?: InputMaybe<DateTimeOperationFilterInput>;
  or?: InputMaybe<Array<ApprovalFlowUserRefFilterInput>>;
  ownershipType?: InputMaybe<ApprovalFlowOwnershipTypeOperationFilterInput>;
  userId?: InputMaybe<StringOperationFilterInput>;
}

export enum ApproveStatus {
  /** 已审批 */
  APPROVED = 'APPROVED',
  /** 待上报/默认 */
  DEFAULT = 'DEFAULT',
  /** 已上报 */
  SUBMITTED = 'SUBMITTED'
}

export interface ApproveStatusOperationFilterInput {
  eq?: InputMaybe<ApproveStatus>;
  in?: InputMaybe<Array<ApproveStatus>>;
  neq?: InputMaybe<ApproveStatus>;
  nin?: InputMaybe<Array<ApproveStatus>>;
}

export interface AssignOrgRequest {
  userOrgsMap: Array<UserOrgMapItemInput>;
}

export interface AssignRoleRequest {
  roles: Array<Scalars['String']['input']>;
  userIds: Array<Scalars['String']['input']>;
}

export enum AssociatedEntityType {
  Object = 'Object'
}

export interface AuthenticateRequest {
  password: Scalars['String']['input'];
  userIdentifier: Scalars['String']['input'];
}

export enum AuthorizationPermission {
  authorization_mutation_authorize = 'authorization_mutation_authorize'
}

export interface AuthorizeRequest {
  allowedPermissions: Array<AppPermission>;
  authorizeTargetType: AuthorizeTargetType;
  /**
   * 授权目标:
   * 用户or角色id
   */
  target: Scalars['String']['input'];
}

export enum AuthorizeTargetType {
  Role = 'Role',
  User = 'User'
}

export enum BlobStorageSettings {
  BlobStorageModuleName = 'BlobStorageModuleName'
}

export enum BlobStorageType {
  Cache = 'Cache',
  Db = 'Db',
  FileSystem = 'FileSystem'
}

export interface BooleanOperationFilterInput {
  eq?: InputMaybe<Scalars['Boolean']['input']>;
  neq?: InputMaybe<Scalars['Boolean']['input']>;
}

export enum CaptchaProvider {
  Image = 'Image',
  Sms = 'Sms'
}

export enum CaptchaType {
  CHINESE = 'CHINESE',
  ENGLISH = 'ENGLISH',
  NUMBER = 'NUMBER',
  NUMBER_AND_LETTER = 'NUMBER_AND_LETTER'
}

export interface ChangePasswordRequest {
  /**
   * 新密码(建议前端二次确认)
   * 注：此处的 Password 应是经过前端哈希处理后的密码
   */
  newPassword: Scalars['String']['input'];
  /**
   * 原密码
   * 注：此处的 Password 应是经过前端哈希处理后的密码
   */
  originPassword: Scalars['String']['input'];
}

export interface ClassEnumOperationFilterInputTypeOfAssociatedEntityTypeFilterInput {
  eq?: InputMaybe<AssociatedEntityType>;
  in?: InputMaybe<Array<InputMaybe<AssociatedEntityType>>>;
  neq?: InputMaybe<AssociatedEntityType>;
  nin?: InputMaybe<Array<InputMaybe<AssociatedEntityType>>>;
}

export interface ClassEnumOperationFilterInputTypeOfBlobStorageTypeFilterInput {
  eq?: InputMaybe<BlobStorageType>;
  in?: InputMaybe<Array<InputMaybe<BlobStorageType>>>;
  neq?: InputMaybe<BlobStorageType>;
  nin?: InputMaybe<Array<InputMaybe<BlobStorageType>>>;
}

export interface ClassEnumOperationFilterInputTypeOfLoginProviderEnumFilterInput {
  eq?: InputMaybe<LoginProviderEnum>;
  in?: InputMaybe<Array<InputMaybe<LoginProviderEnum>>>;
  neq?: InputMaybe<LoginProviderEnum>;
  nin?: InputMaybe<Array<InputMaybe<LoginProviderEnum>>>;
}

export interface ClassEnumOperationFilterInputTypeOfOrgTypeEnumFilterInput {
  eq?: InputMaybe<OrgTypeEnum>;
  in?: InputMaybe<Array<InputMaybe<OrgTypeEnum>>>;
  neq?: InputMaybe<OrgTypeEnum>;
  nin?: InputMaybe<Array<InputMaybe<OrgTypeEnum>>>;
}

export interface CreateApprovalFlowRequest {
  associatedEntityId?: InputMaybe<Scalars['String']['input']>;
  associatedEntityType?: InputMaybe<AssociatedEntityType>;
  description?: InputMaybe<Scalars['String']['input']>;
  name: Scalars['String']['input'];
  nodes: Array<ApprovalFlowNodeDataInput>;
  orgCode: Scalars['String']['input'];
  templateId?: InputMaybe<Scalars['String']['input']>;
}

export interface CreateApprovalFlowTemplateRequest {
  approvalFlowNodeTemplates: Array<ApprovalFlowNodeTemplateDataInput>;
  description: Scalars['String']['input'];
  name: Scalars['String']['input'];
  orgCode: Scalars['String']['input'];
}

export interface CreateBlobObjectRequest {
  file: Scalars['Upload']['input'];
  /** can pass null, will be calculated */
  md5?: InputMaybe<Scalars['String']['input']>;
  storageType: BlobStorageType;
}

export interface CreateGeexEntityGeexRequest {
  name: Scalars['String']['input'];
}

export interface CreateMessageRequest {
  meta?: InputMaybe<Scalars['Any']['input']>;
  severity?: InputMaybe<MessageSeverityType>;
  text: Scalars['String']['input'];
}

export interface CreateOrgRequest {
  code: Scalars['String']['input'];
  createUserId?: InputMaybe<Scalars['String']['input']>;
  name: Scalars['String']['input'];
  orgType?: InputMaybe<OrgTypeEnum>;
}

export interface CreateRoleRequest {
  isDefault?: InputMaybe<Scalars['Boolean']['input']>;
  isStatic?: InputMaybe<Scalars['Boolean']['input']>;
  roleCode: Scalars['String']['input'];
  roleName: Scalars['String']['input'];
}

export interface CreateSquadRequest {
  /** 是否启用设备自动授权，默认false */
  autoAuthorizeDevices?: InputMaybe<Scalars['Boolean']['input']>;
  /** Squad创建者ID（由系统自动设置） */
  creatorId?: InputMaybe<Scalars['String']['input']>;
  /** 邀请码，用于其他用户加入Squad，默认为Squad名称 */
  inviteCode?: InputMaybe<Scalars['String']['input']>;
  /** Squad最大成员数量限制，默认50 */
  maxMemberCount?: InputMaybe<Scalars['Int']['input']>;
  name: Scalars['String']['input'];
}

export interface CreateTenantRequest {
  code: Scalars['String']['input'];
  externalInfo?: InputMaybe<Scalars['Any']['input']>;
  name: Scalars['String']['input'];
}

export interface CreateUserRequest {
  avatarFileId?: InputMaybe<Scalars['String']['input']>;
  claims?: InputMaybe<Array<UserClaimInput>>;
  email?: InputMaybe<Scalars['String']['input']>;
  isEnable?: InputMaybe<Scalars['Boolean']['input']>;
  nickname?: InputMaybe<Scalars['String']['input']>;
  openId?: InputMaybe<Scalars['String']['input']>;
  orgCodes?: InputMaybe<Array<Scalars['String']['input']>>;
  password?: InputMaybe<Scalars['String']['input']>;
  phoneNumber?: InputMaybe<Scalars['String']['input']>;
  provider?: InputMaybe<LoginProviderEnum>;
  roleIds?: InputMaybe<Array<Scalars['String']['input']>>;
  username: Scalars['String']['input'];
}

export enum DataChangeType {
  Org = 'Org',
  Role = 'Role',
  Tenant = 'Tenant',
  User = 'User'
}

export interface DateTimeOperationFilterInput {
  eq?: InputMaybe<Scalars['DateTime']['input']>;
  gt?: InputMaybe<Scalars['DateTime']['input']>;
  gte?: InputMaybe<Scalars['DateTime']['input']>;
  in?: InputMaybe<Array<InputMaybe<Scalars['DateTime']['input']>>>;
  lt?: InputMaybe<Scalars['DateTime']['input']>;
  lte?: InputMaybe<Scalars['DateTime']['input']>;
  neq?: InputMaybe<Scalars['DateTime']['input']>;
  ngt?: InputMaybe<Scalars['DateTime']['input']>;
  ngte?: InputMaybe<Scalars['DateTime']['input']>;
  nin?: InputMaybe<Array<InputMaybe<Scalars['DateTime']['input']>>>;
  nlt?: InputMaybe<Scalars['DateTime']['input']>;
  nlte?: InputMaybe<Scalars['DateTime']['input']>;
}

export interface DeleteBlobObjectRequest {
  ids: Array<Scalars['String']['input']>;
  storageType: BlobStorageType;
}

export interface DeleteMessageDistributionsRequest {
  messageId: Scalars['String']['input'];
  userIds: Array<Scalars['String']['input']>;
}

export interface DeleteUserRequest {
  id: Scalars['String']['input'];
}

export enum DevicePermission {
  quicollab_mutation_authorizeDevice = 'quicollab_mutation_authorizeDevice',
  quicollab_mutation_batchAuthorizeDevices = 'quicollab_mutation_batchAuthorizeDevices',
  quicollab_mutation_batchRevokeDevices = 'quicollab_mutation_batchRevokeDevices',
  quicollab_mutation_revokeDevice = 'quicollab_mutation_revokeDevice',
  quicollab_mutation_setActiveDevice = 'quicollab_mutation_setActiveDevice',
  quicollab_mutation_updateDevice = 'quicollab_mutation_updateDevice'
}

export interface EditApprovalFlowRequest {
  associatedEntityId?: InputMaybe<Scalars['String']['input']>;
  associatedEntityType?: InputMaybe<AssociatedEntityType>;
  description: Scalars['String']['input'];
  id: Scalars['String']['input'];
  name: Scalars['String']['input'];
  nodes: Array<ApprovalFlowNodeDataInput>;
}

export interface EditApprovalFlowTemplateRequest {
  approvalFlowNodeTemplates: Array<ApprovalFlowNodeTemplateDataInput>;
  description: Scalars['String']['input'];
  id: Scalars['String']['input'];
  name: Scalars['String']['input'];
  orgCode?: InputMaybe<Scalars['String']['input']>;
}

export interface EditGeexEntityGeexRequest {
  id: Scalars['String']['input'];
  name?: InputMaybe<Scalars['String']['input']>;
}

export interface EditMessageRequest {
  id: Scalars['String']['input'];
  messageType?: InputMaybe<MessageType>;
  severity?: InputMaybe<MessageSeverityType>;
  text?: InputMaybe<Scalars['String']['input']>;
}

export interface EditSettingRequest {
  name?: InputMaybe<SettingDefinition>;
  scope?: InputMaybe<SettingScopeEnumeration>;
  scopedKey?: InputMaybe<Scalars['String']['input']>;
  value?: InputMaybe<Scalars['Any']['input']>;
}

export interface EditSquadRequest {
  id?: InputMaybe<Scalars['String']['input']>;
  name?: InputMaybe<Scalars['String']['input']>;
}

export interface EditTenantRequest {
  code: Scalars['String']['input'];
  name: Scalars['String']['input'];
}

export interface EditUserRequest {
  avatarFileId?: InputMaybe<Scalars['String']['input']>;
  claims?: InputMaybe<Array<UserClaimInput>>;
  email?: InputMaybe<Scalars['String']['input']>;
  id: Scalars['String']['input'];
  isEnable?: InputMaybe<Scalars['Boolean']['input']>;
  nickname: Scalars['String']['input'];
  orgCodes?: InputMaybe<Array<Scalars['String']['input']>>;
  phoneNumber?: InputMaybe<Scalars['String']['input']>;
  roleIds?: InputMaybe<Array<Scalars['String']['input']>>;
  username?: InputMaybe<Scalars['String']['input']>;
}

export interface FederateAuthenticateRequest {
  /** OAuth Code */
  code: Scalars['String']['input'];
  /** 登陆提供方 */
  loginProvider?: InputMaybe<LoginProviderEnum>;
}

export enum GeexClaimType {
  ClientId = 'ClientId',
  Expires = 'Expires',
  FullName = 'FullName',
  Nickname = 'Nickname',
  Org = 'Org',
  Provider = 'Provider',
  Role = 'Role',
  Sub = 'Sub',
  Tenant = 'Tenant'
}

/** this is a aggregate root of this module, we name it the same as the module feel free to change it to its real name */
export interface GeexEntityGeexFilterInput {
  and?: InputMaybe<Array<GeexEntityGeexFilterInput>>;
  createdOn?: InputMaybe<DateTimeOperationFilterInput>;
  id?: InputMaybe<StringOperationFilterInput>;
  modifiedOn?: InputMaybe<DateTimeOperationFilterInput>;
  name?: InputMaybe<StringOperationFilterInput>;
  or?: InputMaybe<Array<GeexEntityGeexFilterInput>>;
}

export enum GeexEntityGeexPermission {
  geexModGeex_mutation_editGeexEntityGeex = 'geexModGeex_mutation_editGeexEntityGeex',
  geexModGeex_query_geexEntityGeex = 'geexModGeex_query_geexEntityGeex'
}

/** this is a aggregate root of this module, we name it the same as the module feel free to change it to its real name */
export interface GeexEntityGeexSortInput {
  createdOn?: InputMaybe<SortEnumType>;
  id?: InputMaybe<SortEnumType>;
  modifiedOn?: InputMaybe<SortEnumType>;
  name?: InputMaybe<SortEnumType>;
}

/** inherit this enumeration to customise your own business exceptions */
export enum GeexExceptionType {
  Conflict = 'Conflict',
  ExternalError = 'ExternalError',
  NotFound = 'NotFound',
  OnPurpose = 'OnPurpose',
  Unknown = 'Unknown',
  ValidationFailed = 'ValidationFailed'
}

export enum GeexLoginProviders {
  Geex = 'Geex'
}

export enum GeexModGeexPermission {
  geexModGeex_mutation_editGeexEntityGeex = 'geexModGeex_mutation_editGeexEntityGeex',
  geexModGeex_query_geexEntityGeex = 'geexModGeex_query_geexEntityGeex'
}

export enum GeexModGeexSettings {
  GeexModGeexModuleName = 'GeexModGeexModuleName'
}

export enum GeexOrgGeexLoginProviders {
  GeexOrgGeex = 'GeexOrgGeex'
}

export interface GetSettingsRequest {
  _?: InputMaybe<Scalars['String']['input']>;
  filterByName?: InputMaybe<Scalars['String']['input']>;
  scope?: InputMaybe<SettingScopeEnumeration>;
  settingDefinitions?: InputMaybe<Array<SettingDefinition>>;
}

export interface IApproveEntityFilterInput {
  and?: InputMaybe<Array<IApproveEntityFilterInput>>;
  /** 审批操作备注文本 */
  approveRemark?: InputMaybe<StringOperationFilterInput>;
  /** 对象审批状态 */
  approveStatus?: InputMaybe<ApproveStatusOperationFilterInput>;
  or?: InputMaybe<Array<IApproveEntityFilterInput>>;
  /** 是否满足提交条件 */
  submittable?: InputMaybe<BooleanOperationFilterInput>;
}

export interface IApproveEntitySortInput {
  /** 审批操作备注文本 */
  approveRemark?: InputMaybe<SortEnumType>;
  /** 对象审批状态 */
  approveStatus?: InputMaybe<SortEnumType>;
  /** 是否满足提交条件 */
  submittable?: InputMaybe<SortEnumType>;
}

/** Represents a blob object in the storage system */
export interface IBlobObjectFilterInput {
  and?: InputMaybe<Array<IBlobObjectFilterInput>>;
  /** Name of the file */
  fileName?: InputMaybe<StringOperationFilterInput>;
  /** Size of the file in bytes */
  fileSize?: InputMaybe<LongOperationFilterInput>;
  /**
   * The Id property for this entity type.
   * 注意: dbcontext会根据entity是否有id值来判断当前entity是否为新增
   */
  id?: InputMaybe<StringOperationFilterInput>;
  /** MD5 hash of the file content */
  md5?: InputMaybe<StringOperationFilterInput>;
  /** MIME type of the file */
  mimeType?: InputMaybe<StringOperationFilterInput>;
  or?: InputMaybe<Array<IBlobObjectFilterInput>>;
  /** Storage type used for this blob */
  storageType?: InputMaybe<ClassEnumOperationFilterInputTypeOfBlobStorageTypeFilterInput>;
}

/** Represents a blob object in the storage system */
export interface IBlobObjectSortInput {
  /** Expiration time for the blob (if applicable) */
  expireAt?: InputMaybe<SortEnumType>;
  /** Name of the file */
  fileName?: InputMaybe<SortEnumType>;
  /** Size of the file in bytes */
  fileSize?: InputMaybe<SortEnumType>;
  /** MD5 hash of the file content */
  md5?: InputMaybe<SortEnumType>;
  /** MIME type of the file */
  mimeType?: InputMaybe<SortEnumType>;
  /** Storage type used for this blob */
  storageType?: InputMaybe<SortEnumType>;
  /** URL to access the file */
  url?: InputMaybe<SortEnumType>;
}

export interface IMessageContentFilterInput {
  _?: InputMaybe<StringOperationFilterInput>;
  and?: InputMaybe<Array<IMessageContentFilterInput>>;
  or?: InputMaybe<Array<IMessageContentFilterInput>>;
}

/** this is a aggregate root of this module, we name it the same as the module feel free to change it to its real name */
export interface IMessageFilterInput {
  and?: InputMaybe<Array<IMessageFilterInput>>;
  content?: InputMaybe<IMessageContentFilterInput>;
  fromUserId?: InputMaybe<StringOperationFilterInput>;
  /**
   * The Id property for this entity type.
   * 注意: dbcontext会根据entity是否有id值来判断当前entity是否为新增
   */
  id?: InputMaybe<StringOperationFilterInput>;
  messageType?: InputMaybe<MessageTypeOperationFilterInput>;
  or?: InputMaybe<Array<IMessageFilterInput>>;
  severity?: InputMaybe<MessageSeverityTypeOperationFilterInput>;
  time?: InputMaybe<DateTimeOperationFilterInput>;
  title?: InputMaybe<StringOperationFilterInput>;
  toUserIds?: InputMaybe<ListStringOperationFilterInput>;
}

export interface IOrgFilterInput {
  and?: InputMaybe<Array<IOrgFilterInput>>;
  /** 以.作为分割线的编码 */
  code?: InputMaybe<StringOperationFilterInput>;
  name?: InputMaybe<StringOperationFilterInput>;
  or?: InputMaybe<Array<IOrgFilterInput>>;
  /** 组织类型 */
  orgType?: InputMaybe<ClassEnumOperationFilterInputTypeOfOrgTypeEnumFilterInput>;
  /** 父组织编码 */
  parentOrgCode?: InputMaybe<StringOperationFilterInput>;
}

export interface IQuicollabUserFilterInput {
  and?: InputMaybe<Array<IQuicollabUserFilterInput>>;
  devices?: InputMaybe<ListFilterInputTypeOfQuicollabDeviceFilterInput>;
  or?: InputMaybe<Array<IQuicollabUserFilterInput>>;
}

export interface IRoleFilterInput {
  and?: InputMaybe<Array<IRoleFilterInput>>;
  /**
   * The Id property for this entity type.
   * 注意: dbcontext会根据entity是否有id值来判断当前entity是否为新增
   */
  id?: InputMaybe<StringOperationFilterInput>;
  name?: InputMaybe<StringOperationFilterInput>;
  or?: InputMaybe<Array<IRoleFilterInput>>;
  users?: InputMaybe<ListFilterInputTypeOfIUserFilterInput>;
}

export interface ISettingFilterInput {
  and?: InputMaybe<Array<ISettingFilterInput>>;
  /**
   * The Id property for this entity type.
   * 注意: dbcontext会根据entity是否有id值来判断当前entity是否为新增
   */
  id?: InputMaybe<StringOperationFilterInput>;
  name?: InputMaybe<SettingDefinitionOperationFilterInput>;
  or?: InputMaybe<Array<ISettingFilterInput>>;
  scope?: InputMaybe<SettingScopeEnumerationOperationFilterInput>;
  scopedKey?: InputMaybe<StringOperationFilterInput>;
}

export interface ITenantFilterInput {
  and?: InputMaybe<Array<ITenantFilterInput>>;
  code?: InputMaybe<StringOperationFilterInput>;
  externalInfo?: InputMaybe<JsonNodeFilterInput>;
  isEnabled?: InputMaybe<BooleanOperationFilterInput>;
  name?: InputMaybe<StringOperationFilterInput>;
  or?: InputMaybe<Array<ITenantFilterInput>>;
}

export interface IUserFilterInput {
  and?: InputMaybe<Array<IUserFilterInput>>;
  createdOn?: InputMaybe<DateTimeOperationFilterInput>;
  /**
   * The Id property for this entity type.
   * 注意: dbcontext会根据entity是否有id值来判断当前entity是否为新增
   */
  id?: InputMaybe<StringOperationFilterInput>;
  isEnable?: InputMaybe<BooleanOperationFilterInput>;
  nickname?: InputMaybe<StringOperationFilterInput>;
  or?: InputMaybe<Array<IUserFilterInput>>;
  orgCodes?: InputMaybe<ListStringOperationFilterInput>;
  phoneNumber?: InputMaybe<StringOperationFilterInput>;
  roleIds?: InputMaybe<ListStringOperationFilterInput>;
  username?: InputMaybe<StringOperationFilterInput>;
}

export interface IUserSortInput {
  createdOn?: InputMaybe<SortEnumType>;
  /**
   * The Id property for this entity type.
   * 注意: dbcontext会根据entity是否有id值来判断当前entity是否为新增
   */
  id?: InputMaybe<SortEnumType>;
}

export enum IdentityPermission {
  identity_mutation_createOrg = 'identity_mutation_createOrg',
  identity_mutation_createRole = 'identity_mutation_createRole',
  identity_mutation_createUser = 'identity_mutation_createUser',
  identity_mutation_editOrg = 'identity_mutation_editOrg',
  identity_mutation_editRole = 'identity_mutation_editRole',
  identity_mutation_editUser = 'identity_mutation_editUser',
  identity_query_roles = 'identity_query_roles',
  identity_query_users = 'identity_query_users'
}

export interface IntOperationFilterInput {
  eq?: InputMaybe<Scalars['Int']['input']>;
  gt?: InputMaybe<Scalars['Int']['input']>;
  gte?: InputMaybe<Scalars['Int']['input']>;
  in?: InputMaybe<Array<InputMaybe<Scalars['Int']['input']>>>;
  lt?: InputMaybe<Scalars['Int']['input']>;
  lte?: InputMaybe<Scalars['Int']['input']>;
  neq?: InputMaybe<Scalars['Int']['input']>;
  ngt?: InputMaybe<Scalars['Int']['input']>;
  ngte?: InputMaybe<Scalars['Int']['input']>;
  nin?: InputMaybe<Array<InputMaybe<Scalars['Int']['input']>>>;
  nlt?: InputMaybe<Scalars['Int']['input']>;
  nlte?: InputMaybe<Scalars['Int']['input']>;
}

export interface JobExecutionHistoryFilterInput {
  and?: InputMaybe<Array<JobExecutionHistoryFilterInput>>;
  createdOn?: InputMaybe<DateTimeOperationFilterInput>;
  executionEndTime?: InputMaybe<DateTimeOperationFilterInput>;
  executionStartTime?: InputMaybe<DateTimeOperationFilterInput>;
  id?: InputMaybe<StringOperationFilterInput>;
  isSuccess?: InputMaybe<BooleanOperationFilterInput>;
  jobName?: InputMaybe<StringOperationFilterInput>;
  message?: InputMaybe<StringOperationFilterInput>;
  modifiedOn?: InputMaybe<DateTimeOperationFilterInput>;
  or?: InputMaybe<Array<JobExecutionHistoryFilterInput>>;
}

export interface JobStateFilterInput {
  and?: InputMaybe<Array<JobStateFilterInput>>;
  createdOn?: InputMaybe<DateTimeOperationFilterInput>;
  cron?: InputMaybe<StringOperationFilterInput>;
  executionHistories?: InputMaybe<ListFilterInputTypeOfJobExecutionHistoryFilterInput>;
  id?: InputMaybe<StringOperationFilterInput>;
  jobName?: InputMaybe<StringOperationFilterInput>;
  lastExecutionTime?: InputMaybe<DateTimeOperationFilterInput>;
  modifiedOn?: InputMaybe<DateTimeOperationFilterInput>;
  nextExecutionTime?: InputMaybe<DateTimeOperationFilterInput>;
  or?: InputMaybe<Array<JobStateFilterInput>>;
}

export interface JobStateSortInput {
  createdOn?: InputMaybe<SortEnumType>;
  cron?: InputMaybe<SortEnumType>;
  id?: InputMaybe<SortEnumType>;
  jobName?: InputMaybe<SortEnumType>;
  lastExecutionTime?: InputMaybe<SortEnumType>;
  modifiedOn?: InputMaybe<SortEnumType>;
  nextExecutionTime?: InputMaybe<SortEnumType>;
}

export interface JsonNodeFilterInput {
  and?: InputMaybe<Array<JsonNodeFilterInput>>;
  options?: InputMaybe<JsonNodeOptionsFilterInput>;
  or?: InputMaybe<Array<JsonNodeFilterInput>>;
  parent?: InputMaybe<JsonNodeFilterInput>;
  root?: InputMaybe<JsonNodeFilterInput>;
}

export interface JsonNodeOptionsFilterInput {
  and?: InputMaybe<Array<JsonNodeOptionsFilterInput>>;
  or?: InputMaybe<Array<JsonNodeOptionsFilterInput>>;
  propertyNameCaseInsensitive?: InputMaybe<BooleanOperationFilterInput>;
}

export interface ListFilterInputTypeOfApprovalFlowNodeFilterInput {
  all?: InputMaybe<ApprovalFlowNodeFilterInput>;
  any?: InputMaybe<Scalars['Boolean']['input']>;
  none?: InputMaybe<ApprovalFlowNodeFilterInput>;
  some?: InputMaybe<ApprovalFlowNodeFilterInput>;
}

export interface ListFilterInputTypeOfApprovalFlowNodeLogFilterInput {
  all?: InputMaybe<ApprovalFlowNodeLogFilterInput>;
  any?: InputMaybe<Scalars['Boolean']['input']>;
  none?: InputMaybe<ApprovalFlowNodeLogFilterInput>;
  some?: InputMaybe<ApprovalFlowNodeLogFilterInput>;
}

export interface ListFilterInputTypeOfApprovalFlowNodeTemplateFilterInput {
  all?: InputMaybe<ApprovalFlowNodeTemplateFilterInput>;
  any?: InputMaybe<Scalars['Boolean']['input']>;
  none?: InputMaybe<ApprovalFlowNodeTemplateFilterInput>;
  some?: InputMaybe<ApprovalFlowNodeTemplateFilterInput>;
}

export interface ListFilterInputTypeOfApprovalFlowUserRefFilterInput {
  all?: InputMaybe<ApprovalFlowUserRefFilterInput>;
  any?: InputMaybe<Scalars['Boolean']['input']>;
  none?: InputMaybe<ApprovalFlowUserRefFilterInput>;
  some?: InputMaybe<ApprovalFlowUserRefFilterInput>;
}

export interface ListFilterInputTypeOfIOrgFilterInput {
  all?: InputMaybe<IOrgFilterInput>;
  any?: InputMaybe<Scalars['Boolean']['input']>;
  none?: InputMaybe<IOrgFilterInput>;
  some?: InputMaybe<IOrgFilterInput>;
}

export interface ListFilterInputTypeOfIQuicollabUserFilterInput {
  all?: InputMaybe<IQuicollabUserFilterInput>;
  any?: InputMaybe<Scalars['Boolean']['input']>;
  none?: InputMaybe<IQuicollabUserFilterInput>;
  some?: InputMaybe<IQuicollabUserFilterInput>;
}

export interface ListFilterInputTypeOfIRoleFilterInput {
  all?: InputMaybe<IRoleFilterInput>;
  any?: InputMaybe<Scalars['Boolean']['input']>;
  none?: InputMaybe<IRoleFilterInput>;
  some?: InputMaybe<IRoleFilterInput>;
}

export interface ListFilterInputTypeOfIUserFilterInput {
  all?: InputMaybe<IUserFilterInput>;
  any?: InputMaybe<Scalars['Boolean']['input']>;
  none?: InputMaybe<IUserFilterInput>;
  some?: InputMaybe<IUserFilterInput>;
}

export interface ListFilterInputTypeOfJobExecutionHistoryFilterInput {
  all?: InputMaybe<JobExecutionHistoryFilterInput>;
  any?: InputMaybe<Scalars['Boolean']['input']>;
  none?: InputMaybe<JobExecutionHistoryFilterInput>;
  some?: InputMaybe<JobExecutionHistoryFilterInput>;
}

export interface ListFilterInputTypeOfQuicollabDeviceFilterInput {
  all?: InputMaybe<QuicollabDeviceFilterInput>;
  any?: InputMaybe<Scalars['Boolean']['input']>;
  none?: InputMaybe<QuicollabDeviceFilterInput>;
  some?: InputMaybe<QuicollabDeviceFilterInput>;
}

export interface ListFilterInputTypeOfUserClaimFilterInput {
  all?: InputMaybe<UserClaimFilterInput>;
  any?: InputMaybe<Scalars['Boolean']['input']>;
  none?: InputMaybe<UserClaimFilterInput>;
  some?: InputMaybe<UserClaimFilterInput>;
}

export interface ListStringOperationFilterInput {
  all?: InputMaybe<StringOperationFilterInput>;
  any?: InputMaybe<Scalars['Boolean']['input']>;
  none?: InputMaybe<StringOperationFilterInput>;
  some?: InputMaybe<StringOperationFilterInput>;
}

export enum LocalizationSettings {
  LocalizationData = 'LocalizationData',
  LocalizationLanguage = 'LocalizationLanguage'
}

export enum LoginProviderEnum {
  Geex = 'Geex',
  GeexOrgGeex = 'GeexOrgGeex',
  Local = 'Local'
}

export interface LongOperationFilterInput {
  eq?: InputMaybe<Scalars['Long']['input']>;
  gt?: InputMaybe<Scalars['Long']['input']>;
  gte?: InputMaybe<Scalars['Long']['input']>;
  in?: InputMaybe<Array<InputMaybe<Scalars['Long']['input']>>>;
  lt?: InputMaybe<Scalars['Long']['input']>;
  lte?: InputMaybe<Scalars['Long']['input']>;
  neq?: InputMaybe<Scalars['Long']['input']>;
  ngt?: InputMaybe<Scalars['Long']['input']>;
  ngte?: InputMaybe<Scalars['Long']['input']>;
  nin?: InputMaybe<Array<InputMaybe<Scalars['Long']['input']>>>;
  nlt?: InputMaybe<Scalars['Long']['input']>;
  nlte?: InputMaybe<Scalars['Long']['input']>;
}

export interface MarkMessagesReadRequest {
  messageIds: Array<Scalars['String']['input']>;
  userId: Scalars['String']['input'];
}

export enum MessageSeverityType {
  /** Error. */
  ERROR = 'ERROR',
  /** Fatal. */
  FATAL = 'FATAL',
  /** Info. */
  INFO = 'INFO',
  /** Success. */
  SUCCESS = 'SUCCESS',
  /** Warn. */
  WARN = 'WARN'
}

export interface MessageSeverityTypeOperationFilterInput {
  eq?: InputMaybe<MessageSeverityType>;
  in?: InputMaybe<Array<MessageSeverityType>>;
  neq?: InputMaybe<MessageSeverityType>;
  nin?: InputMaybe<Array<MessageSeverityType>>;
}

export enum MessageType {
  /** 用户交互消息, 通常有一个非系统的触发者 */
  INTERACT = 'INTERACT',
  /**
   * 通知, 告知某个信息的消息
   * 区别于单独的toast, 这个消息会留档
   */
  NOTIFICATION = 'NOTIFICATION',
  /** 待办, 带有链接跳转/当前状态等交互功能的消息 */
  TODO = 'TODO'
}

export interface MessageTypeOperationFilterInput {
  eq?: InputMaybe<MessageType>;
  in?: InputMaybe<Array<MessageType>>;
  neq?: InputMaybe<MessageType>;
  nin?: InputMaybe<Array<MessageType>>;
}

export enum MessagingSettings {
  MessagingModuleName = 'MessagingModuleName'
}

export enum MultiTenantPermission {
  multiTenant_mutation_createTenant = 'multiTenant_mutation_createTenant',
  multiTenant_mutation_deleteTenant = 'multiTenant_mutation_deleteTenant',
  multiTenant_mutation_editTenant = 'multiTenant_mutation_editTenant',
  multiTenant_query_tenants = 'multiTenant_query_tenants'
}

export enum OperationType {
  Mutation = 'Mutation',
  Query = 'Query',
  Subscription = 'Subscription'
}

export enum OrgPermission {
  identity_mutation_createOrg = 'identity_mutation_createOrg',
  identity_mutation_editOrg = 'identity_mutation_editOrg'
}

export enum OrgTypeEnum {
  Default = 'Default'
}

export interface QueryApprovalFlowRequest {
  creatorUserId?: InputMaybe<Scalars['String']['input']>;
  endTime?: InputMaybe<Scalars['DateTime']['input']>;
  startTime?: InputMaybe<Scalars['DateTime']['input']>;
  status?: InputMaybe<ApprovalFlowStatus>;
  templateId?: InputMaybe<Scalars['String']['input']>;
}

export interface QueryApprovalFlowTemplateRequest {
  creatorUserId?: InputMaybe<Scalars['String']['input']>;
  endTime?: InputMaybe<Scalars['DateTime']['input']>;
  orgCode?: InputMaybe<Scalars['String']['input']>;
  startTime?: InputMaybe<Scalars['DateTime']['input']>;
}

export interface QueryGeexEntityGeexRequest {
  _?: InputMaybe<Scalars['String']['input']>;
  name?: InputMaybe<Scalars['String']['input']>;
}

export interface QueryJobStatesRequest {
  jobName: Scalars['String']['input'];
}

export interface QuerySquadRequest {
  _?: InputMaybe<Scalars['String']['input']>;
  name?: InputMaybe<Scalars['String']['input']>;
}

export interface QuicollabDeviceFilterInput {
  and?: InputMaybe<Array<QuicollabDeviceFilterInput>>;
  createdOn?: InputMaybe<DateTimeOperationFilterInput>;
  /** 全局设备名称（用于ZeroTier网络中的标识） */
  globalName?: InputMaybe<StringOperationFilterInput>;
  id?: InputMaybe<StringOperationFilterInput>;
  /** 设备是否在线 */
  isOnline?: InputMaybe<BooleanOperationFilterInput>;
  /** 最后上线时间 */
  lastOnlineTime?: InputMaybe<DateTimeOperationFilterInput>;
  modifiedOn?: InputMaybe<DateTimeOperationFilterInput>;
  /** 设备名称 */
  name?: InputMaybe<StringOperationFilterInput>;
  or?: InputMaybe<Array<QuicollabDeviceFilterInput>>;
  /** 设备类型（如：PC、Mobile、Mac等） */
  type?: InputMaybe<StringOperationFilterInput>;
  /** 设备所属用户 */
  user?: InputMaybe<IQuicollabUserFilterInput>;
  /** 设备所属用户ID */
  userId?: InputMaybe<StringOperationFilterInput>;
  /** ZeroTier设备ID，全局唯一 */
  ztNetDeviceId?: InputMaybe<StringOperationFilterInput>;
}

export enum QuicollabPermission {
  quicollab_mutation_addDeviceToSquad = 'quicollab_mutation_addDeviceToSquad',
  quicollab_mutation_addMember = 'quicollab_mutation_addMember',
  quicollab_mutation_authorizeDevice = 'quicollab_mutation_authorizeDevice',
  quicollab_mutation_batchAuthorizeDevices = 'quicollab_mutation_batchAuthorizeDevices',
  quicollab_mutation_batchRevokeDevices = 'quicollab_mutation_batchRevokeDevices',
  quicollab_mutation_createSquad = 'quicollab_mutation_createSquad',
  quicollab_mutation_deleteSquad = 'quicollab_mutation_deleteSquad',
  quicollab_mutation_editSquad = 'quicollab_mutation_editSquad',
  quicollab_mutation_removeDeviceFromSquad = 'quicollab_mutation_removeDeviceFromSquad',
  quicollab_mutation_removeMember = 'quicollab_mutation_removeMember',
  quicollab_mutation_removeMemberAdmin = 'quicollab_mutation_removeMemberAdmin',
  quicollab_mutation_revokeDevice = 'quicollab_mutation_revokeDevice',
  quicollab_mutation_setActiveDevice = 'quicollab_mutation_setActiveDevice',
  quicollab_mutation_setMemberAsAdmin = 'quicollab_mutation_setMemberAsAdmin',
  quicollab_mutation_updateDevice = 'quicollab_mutation_updateDevice',
  quicollab_mutation_updateSquadSettings = 'quicollab_mutation_updateSquadSettings'
}

export enum QuicollabSettings {
  QuicollabModuleName = 'QuicollabModuleName'
}

/** 设备注册请求 */
export interface RegisterDeviceRequest {
  /** 设备名称 */
  name: Scalars['String']['input'];
  /** 设备类型 */
  type: Scalars['String']['input'];
  /** ZeroTier设备ID */
  ztNetMemberId: Scalars['String']['input'];
}

export interface RegisterUserRequest {
  email?: InputMaybe<Scalars['String']['input']>;
  /** 注：此处的 Password 应是经过前端哈希处理后的密码 */
  password: Scalars['String']['input'];
  phoneNumber?: InputMaybe<Scalars['String']['input']>;
  username: Scalars['String']['input'];
}

export interface ResetUserPasswordRequest {
  /**
   * 新密码
   * 注：此处的 Password 应是经过前端哈希处理后的密码
   */
  password: Scalars['String']['input'];
  /** 用户ID */
  userId: Scalars['String']['input'];
}

export enum RolePermission {
  identity_mutation_createRole = 'identity_mutation_createRole',
  identity_mutation_editRole = 'identity_mutation_editRole',
  identity_query_roles = 'identity_query_roles'
}

export enum SchematicsPermission {
  schematics_mutation_editTemplate = 'schematics_mutation_editTemplate',
  schematics_query_template = 'schematics_query_template'
}

export enum SchematicsSettings {
  SchematicsModuleName = 'SchematicsModuleName'
}

export interface SendCaptchaRequest {
  captchaProvider: CaptchaProvider;
  smsCaptchaPhoneNumber?: InputMaybe<Scalars['ChinesePhoneNumber']['input']>;
}

export interface SendNotificationMessageRequest {
  messageId: Scalars['String']['input'];
  toUserIds: Array<Scalars['String']['input']>;
}

export interface SetRoleDefaultRequest {
  roleId: Scalars['String']['input'];
}

export enum SettingDefinition {
  AppAppMenu = 'AppAppMenu',
  AppAppName = 'AppAppName',
  AppPermissions = 'AppPermissions',
  BlobStorageModuleName = 'BlobStorageModuleName',
  GeexModGeexModuleName = 'GeexModGeexModuleName',
  LocalizationData = 'LocalizationData',
  LocalizationLanguage = 'LocalizationLanguage',
  MessagingModuleName = 'MessagingModuleName',
  QuicollabModuleName = 'QuicollabModuleName',
  SchematicsModuleName = 'SchematicsModuleName'
}

export interface SettingDefinitionOperationFilterInput {
  eq?: InputMaybe<SettingDefinition>;
  in?: InputMaybe<Array<InputMaybe<SettingDefinition>>>;
  neq?: InputMaybe<SettingDefinition>;
  nin?: InputMaybe<Array<InputMaybe<SettingDefinition>>>;
}

export enum SettingScopeEnumeration {
  Global = 'Global',
  Tenant = 'Tenant',
  User = 'User'
}

export interface SettingScopeEnumerationOperationFilterInput {
  eq?: InputMaybe<SettingScopeEnumeration>;
  in?: InputMaybe<Array<InputMaybe<SettingScopeEnumeration>>>;
  neq?: InputMaybe<SettingScopeEnumeration>;
  nin?: InputMaybe<Array<InputMaybe<SettingScopeEnumeration>>>;
}

export enum SettingsPermission {
  settings_mutation_editSetting = 'settings_mutation_editSetting'
}

export enum SortEnumType {
  ascend = 'ascend',
  descend = 'descend'
}

/** Squad聚合根 - 远程协作小组 */
export interface SquadFilterInput {
  /** Squad管理员用户ID列表 */
  adminUserIds?: InputMaybe<ListStringOperationFilterInput>;
  and?: InputMaybe<Array<SquadFilterInput>>;
  /** 设备授权状态字典，Key为设备ID，Value为是否已授权 */
  authorizedDevices?: InputMaybe<ListStringOperationFilterInput>;
  /** Squad是否启用设备自动授权 */
  autoAuthorizeDevices?: InputMaybe<BooleanOperationFilterInput>;
  createdOn?: InputMaybe<DateTimeOperationFilterInput>;
  /** Squad创建者ID */
  creatorId?: InputMaybe<StringOperationFilterInput>;
  id?: InputMaybe<StringOperationFilterInput>;
  /** 邀请码（用于加入Squad） */
  inviteCode?: InputMaybe<StringOperationFilterInput>;
  /** Squad最大成员数量限制 */
  maxMemberCount?: InputMaybe<IntOperationFilterInput>;
  modifiedOn?: InputMaybe<DateTimeOperationFilterInput>;
  /** Squad名称 */
  name?: InputMaybe<StringOperationFilterInput>;
  or?: InputMaybe<Array<SquadFilterInput>>;
  /** Squad成员用户ID列表 */
  userIds?: InputMaybe<ListStringOperationFilterInput>;
  /** Squad成员用户, 此处自动将系统默认类型User转换为子类QuicollabUser实体 */
  users?: InputMaybe<ListFilterInputTypeOfIQuicollabUserFilterInput>;
  /** ZeroTier网络ID */
  ztNetNetworkId?: InputMaybe<StringOperationFilterInput>;
}

export enum SquadPermission {
  quicollab_mutation_addDeviceToSquad = 'quicollab_mutation_addDeviceToSquad',
  quicollab_mutation_addMember = 'quicollab_mutation_addMember',
  quicollab_mutation_createSquad = 'quicollab_mutation_createSquad',
  quicollab_mutation_deleteSquad = 'quicollab_mutation_deleteSquad',
  quicollab_mutation_editSquad = 'quicollab_mutation_editSquad',
  quicollab_mutation_removeDeviceFromSquad = 'quicollab_mutation_removeDeviceFromSquad',
  quicollab_mutation_removeMember = 'quicollab_mutation_removeMember',
  quicollab_mutation_removeMemberAdmin = 'quicollab_mutation_removeMemberAdmin',
  quicollab_mutation_setMemberAsAdmin = 'quicollab_mutation_setMemberAsAdmin',
  quicollab_mutation_updateSquadSettings = 'quicollab_mutation_updateSquadSettings'
}

/** Squad聚合根 - 远程协作小组 */
export interface SquadSortInput {
  /** Squad是否启用设备自动授权 */
  autoAuthorizeDevices?: InputMaybe<SortEnumType>;
  createdOn?: InputMaybe<SortEnumType>;
  /** Squad创建者ID */
  creatorId?: InputMaybe<SortEnumType>;
  id?: InputMaybe<SortEnumType>;
  /** 邀请码（用于加入Squad） */
  inviteCode?: InputMaybe<SortEnumType>;
  /** Squad最大成员数量限制 */
  maxMemberCount?: InputMaybe<SortEnumType>;
  modifiedOn?: InputMaybe<SortEnumType>;
  /** Squad名称 */
  name?: InputMaybe<SortEnumType>;
  /** ZeroTier网络ID */
  ztNetNetworkId?: InputMaybe<SortEnumType>;
}

export interface StringOperationFilterInput {
  and?: InputMaybe<Array<StringOperationFilterInput>>;
  contains?: InputMaybe<Scalars['String']['input']>;
  endsWith?: InputMaybe<Scalars['String']['input']>;
  eq?: InputMaybe<Scalars['String']['input']>;
  in?: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>;
  ncontains?: InputMaybe<Scalars['String']['input']>;
  nendsWith?: InputMaybe<Scalars['String']['input']>;
  neq?: InputMaybe<Scalars['String']['input']>;
  nin?: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>;
  nstartsWith?: InputMaybe<Scalars['String']['input']>;
  or?: InputMaybe<Array<StringOperationFilterInput>>;
  startsWith?: InputMaybe<Scalars['String']['input']>;
}

/** this is a aggregate root of this module, we name it the same as the module feel free to change it to its real name */
export enum Template {
  client_admin = 'client_admin',
  client_docs = 'client_docs',
  module = 'module',
  schematics_admin_module = 'schematics_admin_module',
  solution = 'solution'
}

export interface TemplateGenerationArgsInput {
  entityName?: InputMaybe<Scalars['String']['input']>;
  moduleName?: InputMaybe<Scalars['String']['input']>;
  orgName?: InputMaybe<Scalars['String']['input']>;
}

export enum TemplatePermission {
  schematics_mutation_editTemplate = 'schematics_mutation_editTemplate',
  schematics_query_template = 'schematics_query_template'
}

export enum TenantPermission {
  multiTenant_mutation_createTenant = 'multiTenant_mutation_createTenant',
  multiTenant_mutation_deleteTenant = 'multiTenant_mutation_deleteTenant',
  multiTenant_mutation_editTenant = 'multiTenant_mutation_editTenant',
  multiTenant_query_tenants = 'multiTenant_query_tenants'
}

export interface ToggleTenantAvailabilityRequest {
  code: Scalars['String']['input'];
}

export interface UserClaimFilterInput {
  and?: InputMaybe<Array<UserClaimFilterInput>>;
  claimType?: InputMaybe<StringOperationFilterInput>;
  claimValue?: InputMaybe<StringOperationFilterInput>;
  or?: InputMaybe<Array<UserClaimFilterInput>>;
}

export interface UserClaimInput {
  claimType: Scalars['String']['input'];
  claimValue: Scalars['String']['input'];
}

export interface UserFilterInput {
  and?: InputMaybe<Array<UserFilterInput>>;
  avatarFile?: InputMaybe<IBlobObjectFilterInput>;
  avatarFileId?: InputMaybe<StringOperationFilterInput>;
  claims?: InputMaybe<ListFilterInputTypeOfUserClaimFilterInput>;
  createdOn?: InputMaybe<DateTimeOperationFilterInput>;
  email?: InputMaybe<StringOperationFilterInput>;
  id?: InputMaybe<StringOperationFilterInput>;
  isEnable?: InputMaybe<BooleanOperationFilterInput>;
  loginProvider?: InputMaybe<ClassEnumOperationFilterInputTypeOfLoginProviderEnumFilterInput>;
  modifiedOn?: InputMaybe<DateTimeOperationFilterInput>;
  nickname?: InputMaybe<StringOperationFilterInput>;
  openId?: InputMaybe<StringOperationFilterInput>;
  or?: InputMaybe<Array<UserFilterInput>>;
  orgCodes?: InputMaybe<ListStringOperationFilterInput>;
  orgs?: InputMaybe<ListFilterInputTypeOfIOrgFilterInput>;
  /** 注意：此处的 Password 是经过二次哈希处理的密码，前端会单独将密码进行哈希处理后传入, 数据库存储值为二次加盐哈希后的值。 */
  password?: InputMaybe<StringOperationFilterInput>;
  permissions?: InputMaybe<ListStringOperationFilterInput>;
  phoneNumber?: InputMaybe<StringOperationFilterInput>;
  roleIds?: InputMaybe<ListStringOperationFilterInput>;
  roleNames?: InputMaybe<ListStringOperationFilterInput>;
  roles?: InputMaybe<ListFilterInputTypeOfIRoleFilterInput>;
  tenantCode?: InputMaybe<StringOperationFilterInput>;
  username?: InputMaybe<StringOperationFilterInput>;
}

export interface UserOrgMapItemInput {
  orgCodes: Array<Scalars['String']['input']>;
  userId: Scalars['String']['input'];
}

export enum UserPermission {
  identity_mutation_createUser = 'identity_mutation_createUser',
  identity_mutation_editUser = 'identity_mutation_editUser',
  identity_query_users = 'identity_query_users'
}

export interface UserSortInput {
  avatarFile?: InputMaybe<IBlobObjectSortInput>;
  avatarFileId?: InputMaybe<SortEnumType>;
  createdOn?: InputMaybe<SortEnumType>;
  email?: InputMaybe<SortEnumType>;
  id?: InputMaybe<SortEnumType>;
  isEnable?: InputMaybe<SortEnumType>;
  loginProvider?: InputMaybe<SortEnumType>;
  modifiedOn?: InputMaybe<SortEnumType>;
  nickname?: InputMaybe<SortEnumType>;
  openId?: InputMaybe<SortEnumType>;
  /** 注意：此处的 Password 是经过二次哈希处理的密码，前端会单独将密码进行哈希处理后传入, 数据库存储值为二次加盐哈希后的值。 */
  password?: InputMaybe<SortEnumType>;
  phoneNumber?: InputMaybe<SortEnumType>;
  tenantCode?: InputMaybe<SortEnumType>;
  username?: InputMaybe<SortEnumType>;
}

export interface ValidateCaptchaRequest {
  captchaCode: Scalars['String']['input'];
  captchaKey: Scalars['String']['input'];
  captchaProvider: CaptchaProvider;
}
