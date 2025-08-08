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
  ChinesePhoneNumber: { input: string; output: string; }
  DateTime: { input: Date; output: Date; }
  Long: { input: BigInt; output: BigInt; }
  MimeType: { input: string; output: string; }
  ObjectId: { input: string; output: string; }
  Upload: { input: any; output: any; }
}

export enum AppPermission {
  authorization_mutation_authorize = 'authorization_mutation_authorize',
  identity_mutation_createOrg = 'identity_mutation_createOrg',
  identity_mutation_createRole = 'identity_mutation_createRole',
  identity_mutation_createUser = 'identity_mutation_createUser',
  identity_mutation_editOrg = 'identity_mutation_editOrg',
  identity_mutation_editRole = 'identity_mutation_editRole',
  identity_mutation_editUser = 'identity_mutation_editUser',
  identity_query_roles = 'identity_query_roles',
  identity_query_users = 'identity_query_users',
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

export interface AssignOrgRequest {
  userOrgsMap: Array<UserOrgMapItemInput>;
}

export interface AssignRoleRequest {
  roles: Array<Scalars['String']['input']>;
  userIds: Array<Scalars['String']['input']>;
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
  newPassword: Scalars['String']['input'];
  originPassword: Scalars['String']['input'];
}

export interface ClassEnumOperationFilterInputTypeOfBlobStorageTypeFilterInput {
  eq?: InputMaybe<BlobStorageType>;
  in?: InputMaybe<Array<InputMaybe<BlobStorageType>>>;
  neq?: InputMaybe<BlobStorageType>;
  nin?: InputMaybe<Array<InputMaybe<BlobStorageType>>>;
}

export interface ClassEnumOperationFilterInputTypeOfOrgTypeEnumFilterInput {
  eq?: InputMaybe<OrgTypeEnum>;
  in?: InputMaybe<Array<InputMaybe<OrgTypeEnum>>>;
  neq?: InputMaybe<OrgTypeEnum>;
  nin?: InputMaybe<Array<InputMaybe<OrgTypeEnum>>>;
}

export interface CreateBlobObjectRequest {
  file: Scalars['Upload']['input'];
  md5?: InputMaybe<Scalars['String']['input']>;
  storageType: BlobStorageType;
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
  code: Scalars['String']['input'];
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

export interface GetSettingsRequest {
  _?: InputMaybe<Scalars['String']['input']>;
  filterByName?: InputMaybe<Scalars['String']['input']>;
  scope?: InputMaybe<SettingScopeEnumeration>;
  settingDefinitions?: InputMaybe<Array<SettingDefinition>>;
}

export interface IBlobObjectFilterInput {
  and?: InputMaybe<Array<IBlobObjectFilterInput>>;
  fileName?: InputMaybe<StringOperationFilterInput>;
  fileSize?: InputMaybe<LongOperationFilterInput>;
  id?: InputMaybe<StringOperationFilterInput>;
  md5?: InputMaybe<StringOperationFilterInput>;
  mimeType?: InputMaybe<StringOperationFilterInput>;
  or?: InputMaybe<Array<IBlobObjectFilterInput>>;
  storageType?: InputMaybe<ClassEnumOperationFilterInputTypeOfBlobStorageTypeFilterInput>;
}

export interface IMessageContentFilterInput {
  _?: InputMaybe<StringOperationFilterInput>;
  and?: InputMaybe<Array<IMessageContentFilterInput>>;
  or?: InputMaybe<Array<IMessageContentFilterInput>>;
}

export interface IMessageFilterInput {
  and?: InputMaybe<Array<IMessageFilterInput>>;
  content?: InputMaybe<IMessageContentFilterInput>;
  fromUserId?: InputMaybe<StringOperationFilterInput>;
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
  code?: InputMaybe<StringOperationFilterInput>;
  name?: InputMaybe<StringOperationFilterInput>;
  or?: InputMaybe<Array<IOrgFilterInput>>;
  orgType?: InputMaybe<ClassEnumOperationFilterInputTypeOfOrgTypeEnumFilterInput>;
  parentOrgCode?: InputMaybe<StringOperationFilterInput>;
}

export interface IQuicollabUserFilterInput {
  and?: InputMaybe<Array<IQuicollabUserFilterInput>>;
  devices?: InputMaybe<ListFilterInputTypeOfQuicollabDeviceFilterInput>;
  or?: InputMaybe<Array<IQuicollabUserFilterInput>>;
}

export interface IRoleFilterInput {
  and?: InputMaybe<Array<IRoleFilterInput>>;
  id?: InputMaybe<StringOperationFilterInput>;
  name?: InputMaybe<StringOperationFilterInput>;
  or?: InputMaybe<Array<IRoleFilterInput>>;
  users?: InputMaybe<ListFilterInputTypeOfIUserFilterInput>;
}

export interface ISettingFilterInput {
  and?: InputMaybe<Array<ISettingFilterInput>>;
  id?: InputMaybe<StringOperationFilterInput>;
  name?: InputMaybe<SettingDefinitionOperationFilterInput>;
  or?: InputMaybe<Array<ISettingFilterInput>>;
  scope?: InputMaybe<SettingScopeEnumerationOperationFilterInput>;
  scopedKey?: InputMaybe<StringOperationFilterInput>;
}

export interface IUserFilterInput {
  and?: InputMaybe<Array<IUserFilterInput>>;
  createdOn?: InputMaybe<DateTimeOperationFilterInput>;
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

export interface ListFilterInputTypeOfIQuicollabUserFilterInput {
  all?: InputMaybe<IQuicollabUserFilterInput>;
  any?: InputMaybe<Scalars['Boolean']['input']>;
  none?: InputMaybe<IQuicollabUserFilterInput>;
  some?: InputMaybe<IQuicollabUserFilterInput>;
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
  ERROR = 'ERROR',
  FATAL = 'FATAL',
  INFO = 'INFO',
  SUCCESS = 'SUCCESS',
  WARN = 'WARN'
}

export interface MessageSeverityTypeOperationFilterInput {
  eq?: InputMaybe<MessageSeverityType>;
  in?: InputMaybe<Array<MessageSeverityType>>;
  neq?: InputMaybe<MessageSeverityType>;
  nin?: InputMaybe<Array<MessageSeverityType>>;
}

export enum MessageType {
  INTERACT = 'INTERACT',
  NOTIFICATION = 'NOTIFICATION',
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
  password: Scalars['String']['input'];
  phoneNumber?: InputMaybe<Scalars['String']['input']>;
  username: Scalars['String']['input'];
}

export interface ResetUserPasswordRequest {
  password: Scalars['String']['input'];
  userId: Scalars['String']['input'];
}

export enum RolePermission {
  identity_mutation_createRole = 'identity_mutation_createRole',
  identity_mutation_editRole = 'identity_mutation_editRole',
  identity_query_roles = 'identity_query_roles'
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
  LocalizationData = 'LocalizationData',
  LocalizationLanguage = 'LocalizationLanguage',
  MessagingModuleName = 'MessagingModuleName',
  QuicollabModuleName = 'QuicollabModuleName'
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

export interface UserOrgMapItemInput {
  orgCodes: Array<Scalars['String']['input']>;
  userId: Scalars['String']['input'];
}

export enum UserPermission {
  identity_mutation_createUser = 'identity_mutation_createUser',
  identity_mutation_editUser = 'identity_mutation_editUser',
  identity_query_users = 'identity_query_users'
}

export interface ValidateCaptchaRequest {
  captchaCode: Scalars['String']['input'];
  captchaKey: Scalars['String']['input'];
  captchaProvider: CaptchaProvider;
}
