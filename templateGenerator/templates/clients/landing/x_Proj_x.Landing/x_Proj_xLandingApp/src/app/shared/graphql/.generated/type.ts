import { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core';
import gql from 'graphql-tag';
export type Maybe<T> = T | null;
export type Exact<T extends { [key: string]: unknown }> = { [K in keyof T]: T[K] };
export type MakeOptional<T, K extends keyof T> = Omit<T, K> & { [SubKey in K]?: Maybe<T[SubKey]> };
export type MakeMaybe<T, K extends keyof T> = Omit<T, K> & { [SubKey in K]: Maybe<T[SubKey]> };
/** All built-in and custom scalars, mapped to their actual values */
export interface Scalars {
  ID: string;
  String: string;
  Boolean: boolean;
  Int: number;
  Float: number;
  Any: any;
  /** ^\[1\]\(\[3-9\]\)[0-9]{9}$ */
  ChinesePhoneNumberType: string;
  /** The `DateTime` scalar represents an ISO-8601 compliant date time type. */
  DateTime: Date;
  /** The `Long` scalar type represents non-fractional signed whole 64-bit numeric values. Long can represent values between -(2^63) and 2^63 - 1. */
  Long: BigInt;
  ObjectId: string;
  /** The `Upload` scalar type represents a file upload. */
  Upload: any;
}





export enum AppPermission {
  AuthorizationMutationAuthorize = 'authorization_mutation_authorize',
  IdentityMutationCreateOrg = 'identity_mutation_createOrg',
  IdentityMutationCreateRole = 'identity_mutation_createRole',
  IdentityMutationCreateUser = 'identity_mutation_createUser',
  IdentityMutationEditOrg = 'identity_mutation_editOrg',
  IdentityMutationEditRole = 'identity_mutation_editRole',
  IdentityMutationEditUser = 'identity_mutation_editUser',
  IdentityQueryOrgs = 'identity_query_orgs',
  IdentityQueryRoles = 'identity_query_roles',
  IdentityQueryUsers = 'identity_query_users',
  MultiTenantMutationCreateTenant = 'multiTenant_mutation_createTenant',
  MultiTenantMutationDeleteTenant = 'multiTenant_mutation_deleteTenant',
  MultiTenantMutationEditTenant = 'multiTenant_mutation_editTenant',
  MultiTenantQueryTenants = 'multiTenant_query_tenants',
  SettingsMutationEditSetting = 'settings_mutation_editSetting'
}

export enum AppSettings {
  AppAppMenu = 'AppAppMenu',
  AppAppName = 'AppAppName',
  AppPermissions = 'AppPermissions'
}

export enum ApplyPolicy {
  BeforeResolver = 'BEFORE_RESOLVER',
  AfterResolver = 'AFTER_RESOLVER',
  Validation = 'VALIDATION'
}

export interface AssignOrgRequestInput {
  userOrgsMap: Array<UserOrgMapItemInput>;
}

export interface AssignRoleRequestInput {
  userIds: Array<Scalars['String']>;
  roles: Array<Scalars['String']>;
}

export enum AuditStatus {
  Default = 'DEFAULT',
  Submitted = 'SUBMITTED',
  Audited = 'AUDITED'
}

export interface AuditStatusOperationFilterInput {
  eq?: Maybe<AuditStatus>;
  neq?: Maybe<AuditStatus>;
  in?: Maybe<Array<AuditStatus>>;
  nin?: Maybe<Array<AuditStatus>>;
}

export interface AuthenticateInput {
  userIdentifier?: Maybe<Scalars['String']>;
  password?: Maybe<Scalars['String']>;
}

export enum AuthorizationPermission {
  AuthorizationMutationAuthorize = 'authorization_mutation_authorize'
}

export interface AuthorizeInput {
  authorizeTargetType?: Maybe<AuthorizeTargetType>;
  allowedPermissions?: Maybe<Array<Maybe<AppPermission>>>;
  target?: Maybe<Scalars['String']>;
}

export enum AuthorizeTargetType {
  Role = 'Role',
  User = 'User'
}

export interface BlobObject extends IBlobObject, IEntityBase {
  __typename?: 'BlobObject';
  id?: Maybe<Scalars['String']>;
  createdOn: Scalars['DateTime'];
  modifiedOn: Scalars['DateTime'];
  fileName?: Maybe<Scalars['String']>;
  md5?: Maybe<Scalars['String']>;
  url?: Maybe<Scalars['String']>;
  fileSize: Scalars['Long'];
  mimeType?: Maybe<Scalars['String']>;
  storageType?: Maybe<BlobStorageType>;
}

/** A segment of a collection. */
export interface BlobObjectsCollectionSegment {
  __typename?: 'BlobObjectsCollectionSegment';
  /** Information to aid in pagination. */
  pageInfo: CollectionSegmentInfo;
  /** A flattened list of the items. */
  items?: Maybe<Array<Maybe<BlobObject>>>;
  totalCount: Scalars['Int'];
}

export enum BlobStorageSettings {
  BlobStorageModuleName = 'BlobStorageModuleName'
}

export enum BlobStorageType {
  AliyunOss = 'AliyunOss',
  Db = 'Db',
  RedisCache = 'RedisCache'
}

export interface Book extends IEntityBase, IAuditEntity {
  __typename?: 'Book';
  id?: Maybe<Scalars['String']>;
  createdOn: Scalars['DateTime'];
  modifiedOn: Scalars['DateTime'];
  auditStatus: AuditStatus;
  submittable: Scalars['Boolean'];
  name: Scalars['String'];
  auditRemark?: Maybe<Scalars['String']>;
}

export interface BookFilterInput {
  and?: Maybe<Array<BookFilterInput>>;
  or?: Maybe<Array<BookFilterInput>>;
  name?: Maybe<StringOperationFilterInput>;
  auditStatus?: Maybe<AuditStatusOperationFilterInput>;
  auditRemark?: Maybe<StringOperationFilterInput>;
  submittable?: Maybe<BooleanOperationFilterInput>;
  modifiedOn?: Maybe<DateTimeOperationFilterInput>;
  id?: Maybe<StringOperationFilterInput>;
  createdOn?: Maybe<DateTimeOperationFilterInput>;
}

export interface BookSortInput {
  name?: Maybe<SortEnumType>;
  auditStatus?: Maybe<SortEnumType>;
  auditRemark?: Maybe<SortEnumType>;
  submittable?: Maybe<SortEnumType>;
  modifiedOn?: Maybe<SortEnumType>;
  id?: Maybe<SortEnumType>;
  createdOn?: Maybe<SortEnumType>;
}

export enum BookingSettings {
  BookingModuleName = 'BookingModuleName'
}

/** A segment of a collection. */
export interface BooksCollectionSegment {
  __typename?: 'BooksCollectionSegment';
  /** Information to aid in pagination. */
  pageInfo: CollectionSegmentInfo;
  /** A flattened list of the items. */
  items?: Maybe<Array<Maybe<Book>>>;
  totalCount: Scalars['Int'];
}

export interface BooleanOperationFilterInput {
  eq?: Maybe<Scalars['Boolean']>;
  neq?: Maybe<Scalars['Boolean']>;
}

/** 缓存数据变更类型 */
export enum CacheDataType {
  Org = 'Org'
}

export interface Captcha {
  __typename?: 'Captcha';
  captchaType: CaptchaType;
  key: Scalars['String'];
  bitmap?: Maybe<Scalars['String']>;
}

export enum CaptchaProvider {
  Image = 'Image',
  Sms = 'Sms'
}

export enum CaptchaType {
  Number = 'NUMBER',
  English = 'ENGLISH',
  NumberAndLetter = 'NUMBER_AND_LETTER',
  Chinese = 'CHINESE'
}

export interface ChangePasswordRequestInput {
  originPassword: Scalars['String'];
  newPassword: Scalars['String'];
}


export interface ClassEnumOperationFilterInputOfBlobStorageTypeFilterInput {
  eq?: Maybe<BlobStorageType>;
  neq?: Maybe<BlobStorageType>;
  in?: Maybe<Array<Maybe<BlobStorageType>>>;
  nin?: Maybe<Array<Maybe<BlobStorageType>>>;
}

export interface ClassEnumOperationFilterInputOfOrgTypeEnumFilterInput {
  eq?: Maybe<OrgTypeEnum>;
  neq?: Maybe<OrgTypeEnum>;
  in?: Maybe<Array<Maybe<OrgTypeEnum>>>;
  nin?: Maybe<Array<Maybe<OrgTypeEnum>>>;
}

/** Information about the offset pagination. */
export interface CollectionSegmentInfo {
  __typename?: 'CollectionSegmentInfo';
  /** Indicates whether more items exist following the set defined by the clients arguments. */
  hasNextPage: Scalars['Boolean'];
  /** Indicates whether more items exist prior the set defined by the clients arguments. */
  hasPreviousPage: Scalars['Boolean'];
}

export interface CreateBlobObjectRequestInput {
  file?: Maybe<Scalars['Upload']>;
  storageType?: Maybe<BlobStorageType>;
  md5?: Maybe<Scalars['String']>;
}

export interface CreateBookInput {
  name: Scalars['String'];
}

export interface CreateMessageRequestInput {
  text: Scalars['String'];
  severity: MessageSeverityType;
}

export interface CreateOrgInput {
  name: Scalars['String'];
  code: Scalars['String'];
  orgType?: Maybe<OrgTypeEnum>;
  createUserId?: Maybe<Scalars['String']>;
}

export interface CreateRoleInput {
  roleCode: Scalars['String'];
  roleName: Scalars['String'];
  isDefault?: Maybe<Scalars['Boolean']>;
  isStatic?: Maybe<Scalars['Boolean']>;
}

export interface CreateTenantRequestInput {
  code: Scalars['String'];
  name: Scalars['String'];
  externalInfo?: Maybe<Scalars['Any']>;
}

export interface CreateUserRequestInput {
  username: Scalars['String'];
  isEnable: Scalars['Boolean'];
  email?: Maybe<Scalars['String']>;
  roleIds?: Maybe<Array<Scalars['String']>>;
  orgCodes?: Maybe<Array<Scalars['String']>>;
  avatarFileId?: Maybe<Scalars['String']>;
  claims?: Maybe<Array<UserClaimInput>>;
  phoneNumber?: Maybe<Scalars['String']>;
  password?: Maybe<Scalars['String']>;
  nickname?: Maybe<Scalars['String']>;
  openId?: Maybe<Scalars['String']>;
  provider?: Maybe<LoginProviderEnum>;
}

export interface Createx_Aggregate_XInput {
  name: Scalars['String'];
}


export interface DateTimeOperationFilterInput {
  eq?: Maybe<Scalars['DateTime']>;
  neq?: Maybe<Scalars['DateTime']>;
  in?: Maybe<Array<Maybe<Scalars['DateTime']>>>;
  nin?: Maybe<Array<Maybe<Scalars['DateTime']>>>;
  gt?: Maybe<Scalars['DateTime']>;
  ngt?: Maybe<Scalars['DateTime']>;
  gte?: Maybe<Scalars['DateTime']>;
  ngte?: Maybe<Scalars['DateTime']>;
  lt?: Maybe<Scalars['DateTime']>;
  nlt?: Maybe<Scalars['DateTime']>;
  lte?: Maybe<Scalars['DateTime']>;
  nlte?: Maybe<Scalars['DateTime']>;
}

export interface DeleteBlobObjectRequestInput {
  ids?: Maybe<Array<Maybe<Scalars['String']>>>;
  storageType?: Maybe<BlobStorageType>;
}

export interface DeleteMessageDistributionsInput {
  messageId: Scalars['String'];
  userIds: Array<Scalars['String']>;
}

export interface EditBookInput {
  name?: Maybe<Scalars['String']>;
}

export interface EditMessageRequestInput {
  text?: Maybe<Scalars['String']>;
  severity?: Maybe<MessageSeverityType>;
  id: Scalars['String'];
  messageType?: Maybe<MessageType>;
}

export interface EditSettingRequestInput {
  name?: Maybe<SettingDefinition>;
  value?: Maybe<Scalars['Any']>;
  scopedKey?: Maybe<Scalars['String']>;
  scope?: Maybe<SettingScopeEnumeration>;
}

export interface EditTenantRequestInput {
  code: Scalars['String'];
  name: Scalars['String'];
}

export interface EditUserRequestInput {
  id: Scalars['String'];
  isEnable?: Maybe<Scalars['Boolean']>;
  email?: Maybe<Scalars['String']>;
  roleIds: Array<Scalars['String']>;
  orgCodes: Array<Scalars['String']>;
  avatarFileId?: Maybe<Scalars['String']>;
  claims: Array<UserClaimInput>;
  phoneNumber?: Maybe<Scalars['String']>;
  username: Scalars['String'];
}

export interface Editx_Aggregate_XInput {
  name?: Maybe<Scalars['String']>;
}

export interface FederateAuthenticateInput {
  loginProvider?: Maybe<LoginProviderEnum>;
  code?: Maybe<Scalars['String']>;
}

export interface FrontendCall extends IFrontendCall {
  __typename?: 'FrontendCall';
  data?: Maybe<Scalars['Any']>;
  frontendCallType: FrontendCallType;
}

export enum FrontendCallType {
  CacheDataChange = 'CacheDataChange',
  NewMessage = 'NewMessage'
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

export interface GetSettingsInput {
  scope?: Maybe<SettingScopeEnumeration>;
  settingDefinitions?: Maybe<Array<Maybe<SettingDefinition>>>;
  filterByName?: Maybe<Scalars['String']>;
  _?: Maybe<Scalars['String']>;
}

export interface GetUnreadMessagesInput {
  _: Scalars['String'];
}

export interface HintType {
  __typename?: 'HintType';
  _: Scalars['String'];
}

export interface IAuditEntity {
  auditStatus: AuditStatus;
  submittable: Scalars['Boolean'];
  id?: Maybe<Scalars['String']>;
  createdOn: Scalars['DateTime'];
}

export interface IBlobObject {
  fileName?: Maybe<Scalars['String']>;
  md5?: Maybe<Scalars['String']>;
  fileSize: Scalars['Long'];
  mimeType?: Maybe<Scalars['String']>;
  url?: Maybe<Scalars['String']>;
  storageType?: Maybe<BlobStorageType>;
  id?: Maybe<Scalars['String']>;
  createdOn: Scalars['DateTime'];
}

export interface IBlobObjectFilterInput {
  and?: Maybe<Array<IBlobObjectFilterInput>>;
  or?: Maybe<Array<IBlobObjectFilterInput>>;
  id?: Maybe<StringOperationFilterInput>;
  md5?: Maybe<StringOperationFilterInput>;
  mimeType?: Maybe<StringOperationFilterInput>;
  storageType?: Maybe<ClassEnumOperationFilterInputOfBlobStorageTypeFilterInput>;
  fileSize?: Maybe<LongOperationFilterInput>;
  fileName?: Maybe<StringOperationFilterInput>;
}

export interface IEntityBase {
  id?: Maybe<Scalars['String']>;
  createdOn: Scalars['DateTime'];
}

export interface IFrontendCall {
  data?: Maybe<Scalars['Any']>;
  frontendCallType: FrontendCallType;
}

export interface IMessage {
  fromUserId?: Maybe<Scalars['String']>;
  messageType: MessageType;
  content: IMessageContent;
  toUserIds: Array<Scalars['String']>;
  severity: MessageSeverityType;
  title: Scalars['String'];
  time: Scalars['DateTime'];
  id?: Maybe<Scalars['String']>;
  createdOn: Scalars['DateTime'];
}

export interface IMessageContent {
  __typename?: 'IMessageContent';
  _: Scalars['String'];
}

export interface IMessageContentFilterInput {
  and?: Maybe<Array<IMessageContentFilterInput>>;
  or?: Maybe<Array<IMessageContentFilterInput>>;
  _?: Maybe<StringOperationFilterInput>;
}

export interface IMessageFilterInput {
  and?: Maybe<Array<IMessageFilterInput>>;
  or?: Maybe<Array<IMessageFilterInput>>;
  messageType?: Maybe<MessageTypeOperationFilterInput>;
  id?: Maybe<StringOperationFilterInput>;
  fromUserId?: Maybe<StringOperationFilterInput>;
  content?: Maybe<IMessageContentFilterInput>;
  toUserIds?: Maybe<ListStringOperationFilterInput>;
  severity?: Maybe<MessageSeverityTypeOperationFilterInput>;
  title?: Maybe<StringOperationFilterInput>;
  time?: Maybe<DateTimeOperationFilterInput>;
}

export interface IOrg {
  allParentOrgCodes: Array<Scalars['String']>;
  allSubOrgCodes: Array<Scalars['String']>;
  directSubOrgCodes: Array<Scalars['String']>;
  parentOrgCode: Scalars['String'];
  code: Scalars['String'];
  name: Scalars['String'];
  orgType: OrgTypeEnum;
  allParentOrgs: Array<IOrg>;
  allSubOrgs: Array<IOrg>;
  directSubOrgs: Array<IOrg>;
  parentOrg: IOrg;
  id?: Maybe<Scalars['String']>;
  createdOn: Scalars['DateTime'];
}

export interface IPagedList {
  pageIndex: Scalars['Int'];
  pageSize: Scalars['Int'];
  totalPage: Scalars['Int'];
  totalCount: Scalars['Int'];
}

export interface IRole {
  name: Scalars['String'];
  code: Scalars['String'];
  users: Array<IUser>;
  permissions: Array<Scalars['String']>;
  isDefault: Scalars['Boolean'];
  isStatic: Scalars['Boolean'];
  isEnabled: Scalars['Boolean'];
  id?: Maybe<Scalars['String']>;
  createdOn: Scalars['DateTime'];
}

export interface ISetting {
  scope?: Maybe<SettingScopeEnumeration>;
  scopedKey?: Maybe<Scalars['String']>;
  value?: Maybe<Scalars['Any']>;
  name?: Maybe<SettingDefinition>;
  id?: Maybe<Scalars['String']>;
  createdOn: Scalars['DateTime'];
}

export interface ISettingFilterInput {
  and?: Maybe<Array<ISettingFilterInput>>;
  or?: Maybe<Array<ISettingFilterInput>>;
  id?: Maybe<StringOperationFilterInput>;
  name?: Maybe<SettingDefinitionOperationFilterInput>;
  scope?: Maybe<SettingScopeEnumerationOperationFilterInput>;
  scopedKey?: Maybe<StringOperationFilterInput>;
}

export interface ITenant {
  code: Scalars['String'];
  name: Scalars['String'];
  isEnabled: Scalars['Boolean'];
  id?: Maybe<Scalars['String']>;
  createdOn: Scalars['DateTime'];
}

export interface ITenantFilterInput {
  and?: Maybe<Array<ITenantFilterInput>>;
  or?: Maybe<Array<ITenantFilterInput>>;
  code?: Maybe<StringOperationFilterInput>;
  name?: Maybe<StringOperationFilterInput>;
  isEnabled?: Maybe<BooleanOperationFilterInput>;
}

export interface IUser {
  checkPassword: Scalars['Boolean'];
  phoneNumber?: Maybe<Scalars['String']>;
  username: Scalars['String'];
  nickname?: Maybe<Scalars['String']>;
  email?: Maybe<Scalars['String']>;
  loginProvider: LoginProviderEnum;
  openId?: Maybe<Scalars['String']>;
  isEnable: Scalars['Boolean'];
  roleIds: Array<Scalars['String']>;
  orgCodes: Array<Scalars['String']>;
  id?: Maybe<Scalars['String']>;
  createdOn: Scalars['DateTime'];
}


export interface IUserCheckPasswordArgs {
  password: Scalars['String'];
}

export interface IUserFilterInput {
  and?: Maybe<Array<IUserFilterInput>>;
  or?: Maybe<Array<IUserFilterInput>>;
  username?: Maybe<StringOperationFilterInput>;
  nickname?: Maybe<StringOperationFilterInput>;
  isEnable?: Maybe<BooleanOperationFilterInput>;
  phoneNumber?: Maybe<StringOperationFilterInput>;
  orgCodes?: Maybe<ListStringOperationFilterInput>;
  roleIds?: Maybe<ListStringOperationFilterInput>;
  id?: Maybe<StringOperationFilterInput>;
}

export enum IdentityPermission {
  IdentityMutationCreateOrg = 'identity_mutation_createOrg',
  IdentityMutationCreateRole = 'identity_mutation_createRole',
  IdentityMutationCreateUser = 'identity_mutation_createUser',
  IdentityMutationEditOrg = 'identity_mutation_editOrg',
  IdentityMutationEditRole = 'identity_mutation_editRole',
  IdentityMutationEditUser = 'identity_mutation_editUser',
  IdentityQueryOrgs = 'identity_query_orgs',
  IdentityQueryRoles = 'identity_query_roles',
  IdentityQueryUsers = 'identity_query_users'
}

export interface ListFilterInputTypeOfIUserFilterInput {
  all?: Maybe<IUserFilterInput>;
  none?: Maybe<IUserFilterInput>;
  some?: Maybe<IUserFilterInput>;
  any?: Maybe<Scalars['Boolean']>;
}

export interface ListStringOperationFilterInput {
  all?: Maybe<StringOperationFilterInput>;
  none?: Maybe<StringOperationFilterInput>;
  some?: Maybe<StringOperationFilterInput>;
  any?: Maybe<Scalars['Boolean']>;
}

export enum LocalizationSettings {
  LocalizationData = 'LocalizationData',
  LocalizationLanguage = 'LocalizationLanguage'
}

export enum LoginProviderEnum {
  Local = 'Local',
  Trusted = 'Trusted',
  XOrgX = 'x_Org_x'
}


export interface LongOperationFilterInput {
  eq?: Maybe<Scalars['Long']>;
  neq?: Maybe<Scalars['Long']>;
  in?: Maybe<Array<Maybe<Scalars['Long']>>>;
  nin?: Maybe<Array<Maybe<Scalars['Long']>>>;
  gt?: Maybe<Scalars['Long']>;
  ngt?: Maybe<Scalars['Long']>;
  gte?: Maybe<Scalars['Long']>;
  ngte?: Maybe<Scalars['Long']>;
  lt?: Maybe<Scalars['Long']>;
  nlt?: Maybe<Scalars['Long']>;
  lte?: Maybe<Scalars['Long']>;
  nlte?: Maybe<Scalars['Long']>;
}

export interface MarkMessagesReadInput {
  messageIds: Array<Scalars['String']>;
  userId: Scalars['String'];
}

export interface Message extends IMessage, IEntityBase {
  __typename?: 'Message';
  id?: Maybe<Scalars['String']>;
  createdOn: Scalars['DateTime'];
  modifiedOn: Scalars['DateTime'];
  fromUserId?: Maybe<Scalars['String']>;
  messageType: MessageType;
  content: IMessageContent;
  toUserIds: Array<Scalars['String']>;
  title: Scalars['String'];
  time: Scalars['DateTime'];
  severity: MessageSeverityType;
}

export enum MessageSeverityType {
  Info = 'INFO',
  Success = 'SUCCESS',
  Warn = 'WARN',
  Error = 'ERROR',
  Fatal = 'FATAL'
}

export interface MessageSeverityTypeOperationFilterInput {
  eq?: Maybe<MessageSeverityType>;
  neq?: Maybe<MessageSeverityType>;
  in?: Maybe<Array<MessageSeverityType>>;
  nin?: Maybe<Array<MessageSeverityType>>;
}

export enum MessageType {
  Notification = 'NOTIFICATION',
  Todo = 'TODO',
  Interact = 'INTERACT'
}

export interface MessageTypeOperationFilterInput {
  eq?: Maybe<MessageType>;
  neq?: Maybe<MessageType>;
  in?: Maybe<Array<MessageType>>;
  nin?: Maybe<Array<MessageType>>;
}

/** A segment of a collection. */
export interface MessagesCollectionSegment {
  __typename?: 'MessagesCollectionSegment';
  /** Information to aid in pagination. */
  pageInfo: CollectionSegmentInfo;
  /** A flattened list of the items. */
  items?: Maybe<Array<Maybe<Message>>>;
  totalCount: Scalars['Int'];
}

export enum MessagingSettings {
  MessagingModuleName = 'MessagingModuleName'
}

export enum MultiTenantPermission {
  MultiTenantMutationCreateTenant = 'multiTenant_mutation_createTenant',
  MultiTenantMutationDeleteTenant = 'multiTenant_mutation_deleteTenant',
  MultiTenantMutationEditTenant = 'multiTenant_mutation_editTenant',
  MultiTenantQueryTenants = 'multiTenant_query_tenants'
}

export interface Mutation {
  __typename?: 'Mutation';
  _?: Maybe<Scalars['String']>;
  authenticate?: Maybe<UserToken>;
  federateAuthenticate?: Maybe<UserToken>;
  cancelAuthentication: Scalars['Boolean'];
  createTenant: ITenant;
  editTenant: Scalars['Boolean'];
  toggleTenantAvailability: Scalars['Boolean'];
  checkTenant?: Maybe<ITenant>;
  changePassword: Scalars['Boolean'];
  register: Scalars['Boolean'];
  assignRoles: Scalars['Boolean'];
  assignOrgs: Scalars['Boolean'];
  editUser: Scalars['Boolean'];
  createUser: Scalars['Boolean'];
  resetUserPassword: Scalars['Boolean'];
  createOrg: Org;
  fixUserOrg: Scalars['Boolean'];
  createRole: Role;
  setRoleDefault: Scalars['Boolean'];
  markMessagesRead: Scalars['Boolean'];
  deleteMessageDistributions: Scalars['Boolean'];
  sendMessage: Scalars['Boolean'];
  createMessage: IMessage;
  editMessage: Scalars['Boolean'];
  createBlobObject?: Maybe<IBlobObject>;
  deleteBlobObject: Scalars['Boolean'];
  editSetting?: Maybe<ISetting>;
  authorize: Scalars['Boolean'];
  generateCaptcha: Captcha;
  validateCaptcha: Scalars['Boolean'];
  submitx_Aggregate_x?: Maybe<Scalars['Boolean']>;
  auditx_Aggregate_x?: Maybe<Scalars['Boolean']>;
  unsubmitx_Aggregate_x?: Maybe<Scalars['Boolean']>;
  unauditx_Aggregate_x?: Maybe<Scalars['Boolean']>;
  /** 创建x_Aggregate_x */
  createx_Aggregate_x: X_Aggregate_X;
  /** 编辑x_Aggregate_x */
  editx_Aggregate_x: Scalars['Boolean'];
  /** 删除x_Aggregate_x */
  deletex_Aggregate_x: Scalars['Boolean'];
  submitBook?: Maybe<Scalars['Boolean']>;
  auditBook?: Maybe<Scalars['Boolean']>;
  unsubmitBook?: Maybe<Scalars['Boolean']>;
  unauditBook?: Maybe<Scalars['Boolean']>;
  createBook: Book;
  editBook: Scalars['Boolean'];
  deleteBook: Scalars['Boolean'];
}


export interface MutationAuthenticateArgs {
  input?: Maybe<AuthenticateInput>;
}


export interface MutationFederateAuthenticateArgs {
  input?: Maybe<FederateAuthenticateInput>;
}


export interface MutationCreateTenantArgs {
  input: CreateTenantRequestInput;
}


export interface MutationEditTenantArgs {
  input: EditTenantRequestInput;
}


export interface MutationToggleTenantAvailabilityArgs {
  input: ToggleTenantAvailabilityRequestInput;
}


export interface MutationCheckTenantArgs {
  code: Scalars['String'];
}


export interface MutationChangePasswordArgs {
  input: ChangePasswordRequestInput;
}


export interface MutationRegisterArgs {
  input: RegisterUserRequestInput;
}


export interface MutationAssignRolesArgs {
  input: AssignRoleRequestInput;
}


export interface MutationAssignOrgsArgs {
  input: AssignOrgRequestInput;
}


export interface MutationEditUserArgs {
  input: EditUserRequestInput;
}


export interface MutationCreateUserArgs {
  input: CreateUserRequestInput;
}


export interface MutationResetUserPasswordArgs {
  input: ResetUserPasswordRequestInput;
}


export interface MutationCreateOrgArgs {
  input: CreateOrgInput;
}


export interface MutationCreateRoleArgs {
  input: CreateRoleInput;
}


export interface MutationSetRoleDefaultArgs {
  input: SetRoleDefaultInput;
}


export interface MutationMarkMessagesReadArgs {
  input: MarkMessagesReadInput;
}


export interface MutationDeleteMessageDistributionsArgs {
  input: DeleteMessageDistributionsInput;
}


export interface MutationSendMessageArgs {
  input: SendNotificationMessageRequestInput;
}


export interface MutationCreateMessageArgs {
  input: CreateMessageRequestInput;
}


export interface MutationEditMessageArgs {
  input: EditMessageRequestInput;
}


export interface MutationCreateBlobObjectArgs {
  input?: Maybe<CreateBlobObjectRequestInput>;
}


export interface MutationDeleteBlobObjectArgs {
  input?: Maybe<DeleteBlobObjectRequestInput>;
}


export interface MutationEditSettingArgs {
  input?: Maybe<EditSettingRequestInput>;
}


export interface MutationAuthorizeArgs {
  input?: Maybe<AuthorizeInput>;
}


export interface MutationGenerateCaptchaArgs {
  input: SendCaptchaInput;
}


export interface MutationValidateCaptchaArgs {
  input: ValidateCaptchaInput;
}


export interface MutationSubmitx_Aggregate_XArgs {
  ids?: Maybe<Array<Maybe<Scalars['String']>>>;
  remark?: Maybe<Scalars['String']>;
}


export interface MutationAuditx_Aggregate_XArgs {
  ids?: Maybe<Array<Maybe<Scalars['String']>>>;
  remark?: Maybe<Scalars['String']>;
}


export interface MutationUnsubmitx_Aggregate_XArgs {
  ids?: Maybe<Array<Maybe<Scalars['String']>>>;
  remark?: Maybe<Scalars['String']>;
}


export interface MutationUnauditx_Aggregate_XArgs {
  ids?: Maybe<Array<Maybe<Scalars['String']>>>;
  remark?: Maybe<Scalars['String']>;
}


export interface MutationCreatex_Aggregate_XArgs {
  input: Createx_Aggregate_XInput;
}


export interface MutationEditx_Aggregate_XArgs {
  id: Scalars['String'];
  input: Editx_Aggregate_XInput;
}


export interface MutationDeletex_Aggregate_XArgs {
  ids: Array<Scalars['String']>;
}


export interface MutationSubmitBookArgs {
  ids?: Maybe<Array<Maybe<Scalars['String']>>>;
  remark?: Maybe<Scalars['String']>;
}


export interface MutationAuditBookArgs {
  ids?: Maybe<Array<Maybe<Scalars['String']>>>;
  remark?: Maybe<Scalars['String']>;
}


export interface MutationUnsubmitBookArgs {
  ids?: Maybe<Array<Maybe<Scalars['String']>>>;
  remark?: Maybe<Scalars['String']>;
}


export interface MutationUnauditBookArgs {
  ids?: Maybe<Array<Maybe<Scalars['String']>>>;
  remark?: Maybe<Scalars['String']>;
}


export interface MutationCreateBookArgs {
  input: CreateBookInput;
}


export interface MutationEditBookArgs {
  id: Scalars['String'];
  input: EditBookInput;
}


export interface MutationDeleteBookArgs {
  ids: Array<Scalars['String']>;
}


export interface Org extends IEntityBase, IOrg {
  __typename?: 'Org';
  id?: Maybe<Scalars['String']>;
  createdOn: Scalars['DateTime'];
  modifiedOn: Scalars['DateTime'];
  code: Scalars['String'];
  name: Scalars['String'];
  orgType: OrgTypeEnum;
  allSubOrgCodes: Array<Scalars['String']>;
  directSubOrgCodes: Array<Scalars['String']>;
  allSubOrgs: Array<IOrg>;
  directSubOrgs: Array<IOrg>;
  parentOrgCode: Scalars['String'];
  parentOrg: IOrg;
  allParentOrgCodes: Array<Scalars['String']>;
  allParentOrgs: Array<IOrg>;
}

export interface OrgCacheItem {
  __typename?: 'OrgCacheItem';
  orgType?: Maybe<OrgTypeEnum>;
  code?: Maybe<Scalars['String']>;
  name?: Maybe<Scalars['String']>;
  parentOrgCode: Scalars['String'];
}

export interface OrgFilterInput {
  and?: Maybe<Array<OrgFilterInput>>;
  or?: Maybe<Array<OrgFilterInput>>;
  name?: Maybe<StringOperationFilterInput>;
  code?: Maybe<StringOperationFilterInput>;
  parentOrgCode?: Maybe<StringOperationFilterInput>;
  orgType?: Maybe<ClassEnumOperationFilterInputOfOrgTypeEnumFilterInput>;
}

export enum OrgPermission {
  IdentityMutationCreateOrg = 'identity_mutation_createOrg',
  IdentityMutationEditOrg = 'identity_mutation_editOrg',
  IdentityQueryOrgs = 'identity_query_orgs'
}

export enum OrgTypeEnum {
  Default = 'Default'
}

/** A segment of a collection. */
export interface OrgsCollectionSegment {
  __typename?: 'OrgsCollectionSegment';
  /** Information to aid in pagination. */
  pageInfo: CollectionSegmentInfo;
  /** A flattened list of the items. */
  items?: Maybe<Array<Maybe<Org>>>;
  totalCount: Scalars['Int'];
}

export interface Query {
  __typename?: 'Query';
  _?: Maybe<Scalars['String']>;
  tenants?: Maybe<TenantsCollectionSegment>;
  users?: Maybe<UsersCollectionSegment>;
  currentUser: IUser;
  orgs?: Maybe<OrgsCollectionSegment>;
  roles?: Maybe<RolesCollectionSegment>;
  messages?: Maybe<MessagesCollectionSegment>;
  unreadMessages: Array<IMessage>;
  blobObjects?: Maybe<BlobObjectsCollectionSegment>;
  settings?: Maybe<SettingsCollectionSegment>;
  initSettings?: Maybe<Array<Maybe<ISetting>>>;
  myPermissions?: Maybe<Array<Maybe<Scalars['String']>>>;
  _hint?: Maybe<HintType>;
  orgsCache: Array<OrgCacheItem>;
  /** 列表获取_aggregate_ */
  x_aggregate_xs?: Maybe<X_Aggregate_XsCollectionSegment>;
  /** 列表获取_aggregate_ */
  x_aggregate_xById: X_Aggregate_X;
  books?: Maybe<BooksCollectionSegment>;
  bookById: Book;
}


export interface QueryTenantsArgs {
  skip?: Maybe<Scalars['Int']>;
  take?: Maybe<Scalars['Int']>;
  where?: Maybe<ITenantFilterInput>;
}


export interface QueryUsersArgs {
  skip?: Maybe<Scalars['Int']>;
  take?: Maybe<Scalars['Int']>;
  where?: Maybe<IUserFilterInput>;
}


export interface QueryOrgsArgs {
  skip?: Maybe<Scalars['Int']>;
  take?: Maybe<Scalars['Int']>;
  where?: Maybe<OrgFilterInput>;
}


export interface QueryRolesArgs {
  skip?: Maybe<Scalars['Int']>;
  take?: Maybe<Scalars['Int']>;
  where?: Maybe<RoleFilterInput>;
}


export interface QueryMessagesArgs {
  skip?: Maybe<Scalars['Int']>;
  take?: Maybe<Scalars['Int']>;
  where?: Maybe<IMessageFilterInput>;
}


export interface QueryUnreadMessagesArgs {
  input: GetUnreadMessagesInput;
}


export interface QueryBlobObjectsArgs {
  skip?: Maybe<Scalars['Int']>;
  take?: Maybe<Scalars['Int']>;
  where?: Maybe<IBlobObjectFilterInput>;
}


export interface QuerySettingsArgs {
  skip?: Maybe<Scalars['Int']>;
  take?: Maybe<Scalars['Int']>;
  input?: Maybe<GetSettingsInput>;
  where?: Maybe<ISettingFilterInput>;
}


export interface QueryX_Aggregate_XsArgs {
  skip?: Maybe<Scalars['Int']>;
  take?: Maybe<Scalars['Int']>;
  input: Queryx_Aggregate_XInput;
  where?: Maybe<X_Aggregate_XFilterInput>;
  order?: Maybe<Array<X_Aggregate_XSortInput>>;
}


export interface QueryX_Aggregate_XByIdArgs {
  id: Scalars['String'];
}


export interface QueryBooksArgs {
  skip?: Maybe<Scalars['Int']>;
  take?: Maybe<Scalars['Int']>;
  input: QueryBookInput;
  where?: Maybe<BookFilterInput>;
  order?: Maybe<Array<BookSortInput>>;
}


export interface QueryBookByIdArgs {
  id: Scalars['String'];
}

export interface QueryBookInput {
  name?: Maybe<Scalars['String']>;
  _?: Maybe<Scalars['String']>;
}

export interface Queryx_Aggregate_XInput {
  name?: Maybe<Scalars['String']>;
  _?: Maybe<Scalars['String']>;
}

export interface RegisterUserRequestInput {
  password: Scalars['String'];
  username: Scalars['String'];
  phoneNumber?: Maybe<Scalars['String']>;
  email?: Maybe<Scalars['String']>;
}

export interface ResetUserPasswordRequestInput {
  userId: Scalars['String'];
  password: Scalars['String'];
}

export interface Role extends IEntityBase, IRole {
  __typename?: 'Role';
  id?: Maybe<Scalars['String']>;
  createdOn: Scalars['DateTime'];
  modifiedOn: Scalars['DateTime'];
  name: Scalars['String'];
  code: Scalars['String'];
  users: Array<IUser>;
  permissions: Array<Scalars['String']>;
  tenantCode?: Maybe<Scalars['String']>;
  isDefault: Scalars['Boolean'];
  isStatic: Scalars['Boolean'];
  isEnabled: Scalars['Boolean'];
}

export interface RoleFilterInput {
  and?: Maybe<Array<RoleFilterInput>>;
  or?: Maybe<Array<RoleFilterInput>>;
  name?: Maybe<StringOperationFilterInput>;
  id?: Maybe<StringOperationFilterInput>;
  users?: Maybe<ListFilterInputTypeOfIUserFilterInput>;
}

export enum RolePermission {
  IdentityMutationCreateRole = 'identity_mutation_createRole',
  IdentityMutationEditRole = 'identity_mutation_editRole',
  IdentityQueryRoles = 'identity_query_roles'
}

/** A segment of a collection. */
export interface RolesCollectionSegment {
  __typename?: 'RolesCollectionSegment';
  /** Information to aid in pagination. */
  pageInfo: CollectionSegmentInfo;
  /** A flattened list of the items. */
  items?: Maybe<Array<Maybe<Role>>>;
  totalCount: Scalars['Int'];
}

export interface SendCaptchaInput {
  captchaProvider: CaptchaProvider;
  smsCaptchaPhoneNumber?: Maybe<Scalars['ChinesePhoneNumberType']>;
}

export interface SendNotificationMessageRequestInput {
  toUserIds: Array<Scalars['String']>;
  messageId: Scalars['String'];
}

export interface SetRoleDefaultInput {
  roleId: Scalars['String'];
}

export interface Setting extends ISetting, IEntityBase {
  __typename?: 'Setting';
  id?: Maybe<Scalars['String']>;
  createdOn: Scalars['DateTime'];
  modifiedOn: Scalars['DateTime'];
  scope?: Maybe<SettingScopeEnumeration>;
  validScopes?: Maybe<Array<Maybe<SettingScopeEnumeration>>>;
  scopedKey?: Maybe<Scalars['String']>;
  name?: Maybe<SettingDefinition>;
  value?: Maybe<Scalars['Any']>;
}

export enum SettingDefinition {
  AppAppMenu = 'AppAppMenu',
  AppAppName = 'AppAppName',
  AppPermissions = 'AppPermissions',
  BlobStorageModuleName = 'BlobStorageModuleName',
  BookingModuleName = 'BookingModuleName',
  LocalizationData = 'LocalizationData',
  LocalizationLanguage = 'LocalizationLanguage',
  MessagingModuleName = 'MessagingModuleName',
  XModXModuleName = 'x_Mod_xModuleName'
}

export interface SettingDefinitionOperationFilterInput {
  eq?: Maybe<SettingDefinition>;
  neq?: Maybe<SettingDefinition>;
  in?: Maybe<Array<Maybe<SettingDefinition>>>;
  nin?: Maybe<Array<Maybe<SettingDefinition>>>;
}

export enum SettingScopeEnumeration {
  Global = 'Global',
  Tenant = 'Tenant',
  User = 'User'
}

export interface SettingScopeEnumerationOperationFilterInput {
  eq?: Maybe<SettingScopeEnumeration>;
  neq?: Maybe<SettingScopeEnumeration>;
  in?: Maybe<Array<Maybe<SettingScopeEnumeration>>>;
  nin?: Maybe<Array<Maybe<SettingScopeEnumeration>>>;
}

/** A segment of a collection. */
export interface SettingsCollectionSegment {
  __typename?: 'SettingsCollectionSegment';
  /** Information to aid in pagination. */
  pageInfo: CollectionSegmentInfo;
  /** A flattened list of the items. */
  items?: Maybe<Array<Maybe<Setting>>>;
  totalCount: Scalars['Int'];
}

export enum SettingsPermission {
  SettingsMutationEditSetting = 'settings_mutation_editSetting'
}

export enum SortEnumType {
  Asc = 'ASC',
  Desc = 'DESC'
}

export interface StringOperationFilterInput {
  and?: Maybe<Array<StringOperationFilterInput>>;
  or?: Maybe<Array<StringOperationFilterInput>>;
  eq?: Maybe<Scalars['String']>;
  neq?: Maybe<Scalars['String']>;
  contains?: Maybe<Scalars['String']>;
  ncontains?: Maybe<Scalars['String']>;
  in?: Maybe<Array<Maybe<Scalars['String']>>>;
  nin?: Maybe<Array<Maybe<Scalars['String']>>>;
  startsWith?: Maybe<Scalars['String']>;
  nstartsWith?: Maybe<Scalars['String']>;
  endsWith?: Maybe<Scalars['String']>;
  nendsWith?: Maybe<Scalars['String']>;
}

export interface Subscription {
  __typename?: 'Subscription';
  _?: Maybe<Scalars['String']>;
  onFrontendCall: IFrontendCall;
  onBroadcast: IFrontendCall;
  echo: Scalars['String'];
  onCacheDataChange: IFrontendCall;
}


export interface SubscriptionEchoArgs {
  text: Scalars['String'];
}

export interface Tenant extends ITenant, IEntityBase {
  __typename?: 'Tenant';
  id?: Maybe<Scalars['String']>;
  createdOn: Scalars['DateTime'];
  modifiedOn: Scalars['DateTime'];
  code: Scalars['String'];
  name: Scalars['String'];
  isEnabled: Scalars['Boolean'];
  externalInfo?: Maybe<Scalars['Any']>;
}

export enum TenantPermission {
  MultiTenantMutationCreateTenant = 'multiTenant_mutation_createTenant',
  MultiTenantMutationDeleteTenant = 'multiTenant_mutation_deleteTenant',
  MultiTenantMutationEditTenant = 'multiTenant_mutation_editTenant',
  MultiTenantQueryTenants = 'multiTenant_query_tenants'
}

/** A segment of a collection. */
export interface TenantsCollectionSegment {
  __typename?: 'TenantsCollectionSegment';
  /** Information to aid in pagination. */
  pageInfo: CollectionSegmentInfo;
  /** A flattened list of the items. */
  items?: Maybe<Array<ITenant>>;
  totalCount: Scalars['Int'];
}

export interface ToggleTenantAvailabilityRequestInput {
  code: Scalars['String'];
}


export interface User extends IUser, IEntityBase {
  __typename?: 'User';
  id?: Maybe<Scalars['String']>;
  createdOn: Scalars['DateTime'];
  modifiedOn: Scalars['DateTime'];
  avatarFile?: Maybe<IBlobObject>;
  claims: Array<UserClaim>;
  checkPassword: Scalars['Boolean'];
  setPassword: User;
  phoneNumber?: Maybe<Scalars['String']>;
  isEnable: Scalars['Boolean'];
  username: Scalars['String'];
  nickname?: Maybe<Scalars['String']>;
  email?: Maybe<Scalars['String']>;
  password: Scalars['String'];
  orgs: Array<IOrg>;
  orgCodes: Array<Scalars['String']>;
  permissions: Array<Scalars['String']>;
  roleIds: Array<Scalars['String']>;
  avatarFileId?: Maybe<Scalars['String']>;
  roles: Array<IRole>;
  roleNames: Array<Scalars['String']>;
  loginProvider: LoginProviderEnum;
  openId?: Maybe<Scalars['String']>;
  tenantCode?: Maybe<Scalars['String']>;
}


export interface UserClaimsArgs {
  where?: Maybe<UserClaimFilterInput>;
}


export interface UserCheckPasswordArgs {
  password: Scalars['String'];
}


export interface UserSetPasswordArgs {
  password?: Maybe<Scalars['String']>;
}

export interface UserClaim {
  __typename?: 'UserClaim';
  claimType: Scalars['String'];
  claimValue: Scalars['String'];
}

export interface UserClaimFilterInput {
  and?: Maybe<Array<UserClaimFilterInput>>;
  or?: Maybe<Array<UserClaimFilterInput>>;
  claimType?: Maybe<StringOperationFilterInput>;
  claimValue?: Maybe<StringOperationFilterInput>;
}

export interface UserClaimInput {
  claimType: Scalars['String'];
  claimValue: Scalars['String'];
}

export interface UserOrgMapItemInput {
  userId: Scalars['String'];
  orgCodes: Array<Scalars['String']>;
}

export enum UserPermission {
  IdentityMutationCreateUser = 'identity_mutation_createUser',
  IdentityMutationEditUser = 'identity_mutation_editUser',
  IdentityQueryUsers = 'identity_query_users'
}

export interface UserToken {
  __typename?: 'UserToken';
  token?: Maybe<Scalars['String']>;
  user?: Maybe<IUser>;
  loginProvider?: Maybe<LoginProviderEnum>;
  userId: Scalars['String'];
  name: Scalars['String'];
}

/** A segment of a collection. */
export interface UsersCollectionSegment {
  __typename?: 'UsersCollectionSegment';
  /** Information to aid in pagination. */
  pageInfo: CollectionSegmentInfo;
  /** A flattened list of the items. */
  items?: Maybe<Array<Maybe<User>>>;
  totalCount: Scalars['Int'];
}

export interface ValidateCaptchaInput {
  captchaKey: Scalars['String'];
  captchaProvider: CaptchaProvider;
  captchaCode: Scalars['String'];
}

/** A segment of a collection. */
export interface X_Aggregate_XsCollectionSegment {
  __typename?: 'X_aggregate_xsCollectionSegment';
  /** Information to aid in pagination. */
  pageInfo: CollectionSegmentInfo;
  /** A flattened list of the items. */
  items?: Maybe<Array<Maybe<X_Aggregate_X>>>;
  totalCount: Scalars['Int'];
}

export enum _Mod_Settings {
  XModXModuleName = 'x_Mod_xModuleName'
}

/** this is a aggregate root of this module, we name it the same as the module feel free to change it to its real name */
export interface X_Aggregate_X extends IEntityBase, IAuditEntity {
  __typename?: 'x_Aggregate_x';
  id?: Maybe<Scalars['String']>;
  createdOn: Scalars['DateTime'];
  modifiedOn: Scalars['DateTime'];
  auditStatus: AuditStatus;
  submittable: Scalars['Boolean'];
  name: Scalars['String'];
  auditRemark?: Maybe<Scalars['String']>;
}

/** this is a aggregate root of this module, we name it the same as the module feel free to change it to its real name */
export interface X_Aggregate_XFilterInput {
  and?: Maybe<Array<X_Aggregate_XFilterInput>>;
  or?: Maybe<Array<X_Aggregate_XFilterInput>>;
  name?: Maybe<StringOperationFilterInput>;
  auditStatus?: Maybe<AuditStatusOperationFilterInput>;
  auditRemark?: Maybe<StringOperationFilterInput>;
  submittable?: Maybe<BooleanOperationFilterInput>;
  modifiedOn?: Maybe<DateTimeOperationFilterInput>;
  id?: Maybe<StringOperationFilterInput>;
  createdOn?: Maybe<DateTimeOperationFilterInput>;
}

/** this is a aggregate root of this module, we name it the same as the module feel free to change it to its real name */
export interface X_Aggregate_XSortInput {
  name?: Maybe<SortEnumType>;
  auditStatus?: Maybe<SortEnumType>;
  auditRemark?: Maybe<SortEnumType>;
  submittable?: Maybe<SortEnumType>;
  modifiedOn?: Maybe<SortEnumType>;
  id?: Maybe<SortEnumType>;
  createdOn?: Maybe<SortEnumType>;
}

export enum X_Proj_XFrontCallType {
  CacheDataChange = 'CacheDataChange'
}

export enum X_Proj_XLoginProviderEnum {
  XOrgX = 'x_Org_x'
}

export type BooksQueryVariables = Exact<{
  input: QueryBookInput;
  skip?: Maybe<Scalars['Int']>;
  take?: Maybe<Scalars['Int']>;
  where?: Maybe<BookFilterInput>;
  order?: Maybe<Array<BookSortInput> | BookSortInput>;
}>;


export type BooksQuery = (
  { __typename?: 'Query' }
  & { books?: Maybe<(
    { __typename?: 'BooksCollectionSegment' }
    & Pick<BooksCollectionSegment, 'totalCount'>
    & { items?: Maybe<Array<Maybe<(
      { __typename?: 'Book' }
      & BookBriefFragment
    )>>>, pageInfo: (
      { __typename?: 'CollectionSegmentInfo' }
      & PageInfoFragment
    ) }
  )> }
);

export type BookByIdQueryVariables = Exact<{
  id: Scalars['String'];
}>;


export type BookByIdQuery = (
  { __typename?: 'Query' }
  & { bookById: (
    { __typename?: 'Book' }
    & BookDetailFragment
  ) }
);

export type CreateBooksMutationVariables = Exact<{
  input: CreateBookInput;
}>;


export type CreateBooksMutation = (
  { __typename?: 'Mutation' }
  & { createBook: (
    { __typename?: 'Book' }
    & Pick<Book, 'id'>
  ) }
);

export type DeleteBooksMutationVariables = Exact<{
  ids: Array<Scalars['String']> | Scalars['String'];
}>;


export type DeleteBooksMutation = (
  { __typename?: 'Mutation' }
  & Pick<Mutation, 'deleteBook'>
);

export type EditBooksMutationVariables = Exact<{
  id: Scalars['String'];
  input: EditBookInput;
}>;


export type EditBooksMutation = (
  { __typename?: 'Mutation' }
  & Pick<Mutation, 'editBook'>
);

export type AuditBookMutationVariables = Exact<{
  ids?: Maybe<Array<Maybe<Scalars['String']>> | Maybe<Scalars['String']>>;
}>;


export type AuditBookMutation = (
  { __typename?: 'Mutation' }
  & Pick<Mutation, 'auditBook'>
);

export type UnauditBookMutationVariables = Exact<{
  ids?: Maybe<Array<Maybe<Scalars['String']>> | Maybe<Scalars['String']>>;
}>;


export type UnauditBookMutation = (
  { __typename?: 'Mutation' }
  & Pick<Mutation, 'unauditBook'>
);

export type SubmitBooksMutationVariables = Exact<{
  ids?: Maybe<Array<Maybe<Scalars['String']>> | Maybe<Scalars['String']>>;
}>;


export type SubmitBooksMutation = (
  { __typename?: 'Mutation' }
  & Pick<Mutation, 'submitBook'>
);

export type UnsubmitBooksMutationVariables = Exact<{
  ids?: Maybe<Array<Maybe<Scalars['String']>> | Maybe<Scalars['String']>>;
}>;


export type UnsubmitBooksMutation = (
  { __typename?: 'Mutation' }
  & Pick<Mutation, 'unsubmitBook'>
);

export type BookBriefFragment = (
  { __typename?: 'Book' }
  & Pick<Book, 'id' | 'name' | 'auditStatus' | 'createdOn'>
);

export type BookDetailFragment = (
  { __typename?: 'Book' }
  & Pick<Book, 'id' | 'name' | 'auditStatus' | 'createdOn'>
);

export type RoleBriefFragment = (
  { __typename?: 'Role' }
  & Pick<Role, 'createdOn' | 'name' | 'id' | 'isStatic' | 'isDefault'>
);

export type RoleDetailFragment = (
  { __typename?: 'Role' }
  & Pick<Role, 'permissions' | 'name'>
  & { users: Array<(
    { __typename?: 'User' }
    & Pick<User, 'id'>
  )> }
  & RoleBriefFragment
);

export type RoleMinimalFragment = (
  { __typename?: 'Role' }
  & Pick<Role, 'id' | 'name'>
);

export type OrgBriefFragment = (
  { __typename?: 'Org' }
  & Pick<Org, 'code' | 'name' | 'orgType' | 'parentOrgCode' | 'id'>
);

export type OrgDetailFragment = (
  { __typename?: 'Org' }
  & { allSubOrgs: Array<(
    { __typename?: 'Org' }
    & Pick<Org, 'name' | 'code'>
  )>, directSubOrgs: Array<(
    { __typename?: 'Org' }
    & Pick<Org, 'name' | 'code'>
  )> }
  & OrgBriefFragment
);

export type UserBriefFragment = (
  { __typename?: 'User' }
  & Pick<User, 'id' | 'username' | 'nickname' | 'phoneNumber' | 'email' | 'isEnable' | 'openId' | 'loginProvider' | 'roleNames' | 'roleIds'>
);

export type UserListFragment = (
  { __typename?: 'User' }
  & Pick<User, 'createdOn' | 'orgCodes'>
  & UserBriefFragment
);

export type UserCacheDtoFragment = (
  { __typename?: 'User' }
  & { avatarFile?: Maybe<(
    { __typename?: 'BlobObject' }
    & Pick<BlobObject, 'url'>
  )> }
  & UserBriefFragment
);

export type UserMinimalFragment = (
  { __typename?: 'User' }
  & Pick<User, 'id' | 'openId' | 'username' | 'nickname'>
);

export type OrgRecursiveParentFragment = (
  { __typename?: 'Org' }
  & { parentOrg: (
    { __typename?: 'Org' }
    & { parentOrg: (
      { __typename?: 'Org' }
      & { parentOrg: (
        { __typename?: 'Org' }
        & { parentOrg: (
          { __typename?: 'Org' }
          & { parentOrg: (
            { __typename?: 'Org' }
            & { parentOrg: (
              { __typename?: 'Org' }
              & { parentOrg: (
                { __typename?: 'Org' }
                & { parentOrg: (
                  { __typename?: 'Org' }
                  & { parentOrg: (
                    { __typename?: 'Org' }
                    & { parentOrg: (
                      { __typename?: 'Org' }
                      & OrgBriefFragment
                    ) }
                    & OrgBriefFragment
                  ) }
                  & OrgBriefFragment
                ) }
                & OrgBriefFragment
              ) }
              & OrgBriefFragment
            ) }
            & OrgBriefFragment
          ) }
          & OrgBriefFragment
        ) }
        & OrgBriefFragment
      ) }
      & OrgBriefFragment
    ) }
    & OrgBriefFragment
  ) }
);

export type UserDetailFragment = (
  { __typename?: 'User' }
  & Pick<User, 'isEnable' | 'permissions' | 'orgCodes' | 'avatarFileId'>
  & { avatarFile?: Maybe<(
    { __typename?: 'BlobObject' }
    & Pick<BlobObject, 'url'>
    & BlobObjectBriefFragment
  )>, orgs: Array<(
    { __typename?: 'Org' }
    & Pick<Org, 'name' | 'code'>
    & { allParentOrgs: Array<(
      { __typename?: 'Org' }
      & Pick<Org, 'code' | 'name'>
    )> }
  )>, claims: Array<(
    { __typename?: 'UserClaim' }
    & Pick<UserClaim, 'claimType' | 'claimValue'>
  )> }
  & UserBriefFragment
);

export type UserListsQueryVariables = Exact<{
  skip?: Maybe<Scalars['Int']>;
  take?: Maybe<Scalars['Int']>;
  where?: Maybe<IUserFilterInput>;
}>;


export type UserListsQuery = (
  { __typename?: 'Query' }
  & { users?: Maybe<(
    { __typename?: 'UsersCollectionSegment' }
    & Pick<UsersCollectionSegment, 'totalCount'>
    & { items?: Maybe<Array<Maybe<(
      { __typename?: 'User' }
      & UserListFragment
    )>>>, pageInfo: (
      { __typename?: 'CollectionSegmentInfo' }
      & PageInfoFragment
    ) }
  )> }
);

export type UserByIdQueryVariables = Exact<{
  id?: Maybe<Scalars['String']>;
}>;


export type UserByIdQuery = (
  { __typename?: 'Query' }
  & { users?: Maybe<(
    { __typename?: 'UsersCollectionSegment' }
    & { items?: Maybe<Array<Maybe<(
      { __typename?: 'User' }
      & UserDetailFragment
    )>>> }
  )> }
);

export type UserMenusQueryVariables = Exact<{
  where?: Maybe<IUserFilterInput>;
}>;


export type UserMenusQuery = (
  { __typename?: 'Query' }
  & { users?: Maybe<(
    { __typename?: 'UsersCollectionSegment' }
    & { items?: Maybe<Array<Maybe<(
      { __typename?: 'User' }
      & UserMinimalFragment
    )>>> }
  )> }
);

export type EditUserMutationVariables = Exact<{
  input: EditUserRequestInput;
}>;


export type EditUserMutation = (
  { __typename?: 'Mutation' }
  & Pick<Mutation, 'editUser'>
);

export type CreateUserMutationVariables = Exact<{
  input: CreateUserRequestInput;
}>;


export type CreateUserMutation = (
  { __typename?: 'Mutation' }
  & Pick<Mutation, 'createUser'>
);

export type ResetUserPasswordMutationVariables = Exact<{
  input: ResetUserPasswordRequestInput;
}>;


export type ResetUserPasswordMutation = (
  { __typename?: 'Mutation' }
  & Pick<Mutation, 'resetUserPassword'>
);

export type ChangePasswordMutationVariables = Exact<{
  input: ChangePasswordRequestInput;
}>;


export type ChangePasswordMutation = (
  { __typename?: 'Mutation' }
  & Pick<Mutation, 'changePassword'>
);

export type RoleListsQueryVariables = Exact<{
  skip?: Maybe<Scalars['Int']>;
  take?: Maybe<Scalars['Int']>;
  where?: Maybe<RoleFilterInput>;
}>;


export type RoleListsQuery = (
  { __typename?: 'Query' }
  & { roles?: Maybe<(
    { __typename?: 'RolesCollectionSegment' }
    & Pick<RolesCollectionSegment, 'totalCount'>
    & { items?: Maybe<Array<Maybe<(
      { __typename?: 'Role' }
      & RoleBriefFragment
    )>>>, pageInfo: (
      { __typename?: 'CollectionSegmentInfo' }
      & PageInfoFragment
    ) }
  )> }
);

export type RoleMenusQueryVariables = Exact<{
  where?: Maybe<RoleFilterInput>;
}>;


export type RoleMenusQuery = (
  { __typename?: 'Query' }
  & { roles?: Maybe<(
    { __typename?: 'RolesCollectionSegment' }
    & { items?: Maybe<Array<Maybe<(
      { __typename?: 'Role' }
      & RoleMinimalFragment
    )>>> }
  )> }
);

export type RoleByNameQueryVariables = Exact<{
  name?: Maybe<Scalars['String']>;
}>;


export type RoleByNameQuery = (
  { __typename?: 'Query' }
  & { roles?: Maybe<(
    { __typename?: 'RolesCollectionSegment' }
    & { items?: Maybe<Array<Maybe<(
      { __typename?: 'Role' }
      & RoleDetailFragment
    )>>> }
  )> }
);

export type RoleByIdQueryVariables = Exact<{
  id?: Maybe<Scalars['String']>;
}>;


export type RoleByIdQuery = (
  { __typename?: 'Query' }
  & { roles?: Maybe<(
    { __typename?: 'RolesCollectionSegment' }
    & { items?: Maybe<Array<Maybe<(
      { __typename?: 'Role' }
      & RoleDetailFragment
    )>>> }
  )> }
);

export type CreateRoleMutationVariables = Exact<{
  input: CreateRoleInput;
}>;


export type CreateRoleMutation = (
  { __typename?: 'Mutation' }
  & { createRole: (
    { __typename?: 'Role' }
    & Pick<Role, 'id' | 'createdOn' | 'name' | 'permissions'>
    & { users: Array<(
      { __typename?: 'User' }
      & Pick<User, 'permissions' | 'id' | 'username' | 'email' | 'phoneNumber'>
    )> }
  ) }
);

export type AuthorizeMutationVariables = Exact<{
  input: AuthorizeInput;
}>;


export type AuthorizeMutation = (
  { __typename?: 'Mutation' }
  & Pick<Mutation, 'authorize'>
);

export type OrgsQueryVariables = Exact<{
  where?: Maybe<OrgFilterInput>;
}>;


export type OrgsQuery = (
  { __typename?: 'Query' }
  & { orgs?: Maybe<(
    { __typename?: 'OrgsCollectionSegment' }
    & { items?: Maybe<Array<Maybe<(
      { __typename?: 'Org' }
      & OrgBriefFragment
    )>>> }
  )> }
);

export type CreateOrgMutationVariables = Exact<{
  input: CreateOrgInput;
}>;


export type CreateOrgMutation = (
  { __typename?: 'Mutation' }
  & { createOrg: (
    { __typename?: 'Org' }
    & OrgBriefFragment
  ) }
);

export type AssignOrgsMutationVariables = Exact<{
  input: AssignOrgRequestInput;
}>;


export type AssignOrgsMutation = (
  { __typename?: 'Mutation' }
  & Pick<Mutation, 'assignOrgs'>
);

export type SetRoleDefaultMutationVariables = Exact<{
  roleId: Scalars['String'];
}>;


export type SetRoleDefaultMutation = (
  { __typename?: 'Mutation' }
  & Pick<Mutation, 'setRoleDefault'>
);

export type MessageBriefFragment = (
  { __typename?: 'Message' }
  & Pick<Message, 'fromUserId' | 'id' | 'messageType' | 'severity' | 'time' | 'title'>
);

export type MessageDetailFragment = (
  { __typename?: 'Message' }
  & Pick<Message, 'toUserIds' | 'createdOn'>
  & { content: (
    { __typename?: 'IMessageContent' }
    & Pick<IMessageContent, '_'>
  ) }
);

export type CreateMessageMutationVariables = Exact<{
  input: CreateMessageRequestInput;
}>;


export type CreateMessageMutation = (
  { __typename?: 'Mutation' }
  & { createMessage: (
    { __typename?: 'Message' }
    & Pick<Message, 'id'>
  ) }
);

export type EditMessageMutationVariables = Exact<{
  input: EditMessageRequestInput;
}>;


export type EditMessageMutation = (
  { __typename?: 'Mutation' }
  & Pick<Mutation, 'editMessage'>
);

export type SendMessageMutationVariables = Exact<{
  input: SendNotificationMessageRequestInput;
}>;


export type SendMessageMutation = (
  { __typename?: 'Mutation' }
  & Pick<Mutation, 'sendMessage'>
);

export type MessagesQueryVariables = Exact<{
  skip?: Maybe<Scalars['Int']>;
  take?: Maybe<Scalars['Int']>;
  where?: Maybe<IMessageFilterInput>;
  includeDetail: Scalars['Boolean'];
}>;


export type MessagesQuery = (
  { __typename?: 'Query' }
  & { messages?: Maybe<(
    { __typename?: 'MessagesCollectionSegment' }
    & Pick<MessagesCollectionSegment, 'totalCount'>
    & { items?: Maybe<Array<Maybe<(
      { __typename?: 'Message' }
      & MessageBriefFragment
      & MessageDetailFragment
    )>>>, pageInfo: (
      { __typename?: 'CollectionSegmentInfo' }
      & PageInfoFragment
    ) }
  )> }
);

export type AuthenticateResultFragmentFragment = (
  { __typename?: 'UserToken' }
  & Pick<UserToken, 'userId' | 'token'>
  & { user?: Maybe<(
    { __typename?: 'User' }
    & UserDetailFragment
  )> }
);

export type AuthenticateMutationVariables = Exact<{
  input: AuthenticateInput;
}>;


export type AuthenticateMutation = (
  { __typename?: 'Mutation' }
  & { authenticate?: Maybe<(
    { __typename?: 'UserToken' }
    & AuthenticateResultFragmentFragment
  )> }
);

export type FederateAuthenticateMutationVariables = Exact<{
  code: Scalars['String'];
  loginProvider: LoginProviderEnum;
}>;


export type FederateAuthenticateMutation = (
  { __typename?: 'Mutation' }
  & { federateAuthenticate?: Maybe<(
    { __typename?: 'UserToken' }
    & AuthenticateResultFragmentFragment
  )> }
);

export type RegisterAndSignInMutationVariables = Exact<{
  registerInput: RegisterUserRequestInput;
  authenticateInput: AuthenticateInput;
}>;


export type RegisterAndSignInMutation = (
  { __typename?: 'Mutation' }
  & Pick<Mutation, 'register'>
  & { authenticate?: Maybe<(
    { __typename?: 'UserToken' }
    & Pick<UserToken, 'userId' | 'token'>
    & { user?: Maybe<(
      { __typename?: 'User' }
      & Pick<User, 'roleNames' | 'roleIds' | 'permissions' | 'id' | 'phoneNumber' | 'email' | 'username'>
      & { avatarFile?: Maybe<(
        { __typename?: 'BlobObject' }
        & Pick<BlobObject, 'url'>
      )> }
    )> }
  )> }
);

export type SendSmsCaptchaMutationVariables = Exact<{
  phoneOrEmail: Scalars['ChinesePhoneNumberType'];
}>;


export type SendSmsCaptchaMutation = (
  { __typename?: 'Mutation' }
  & { generateCaptcha: (
    { __typename?: 'Captcha' }
    & Pick<Captcha, 'captchaType' | 'key'>
  ) }
);

export type ValidateSmsCaptchaMutationVariables = Exact<{
  captchaKey: Scalars['String'];
  captchaCode: Scalars['String'];
}>;


export type ValidateSmsCaptchaMutation = (
  { __typename?: 'Mutation' }
  & Pick<Mutation, 'validateCaptcha'>
);

export type TenantsQueryVariables = Exact<{
  skip?: Maybe<Scalars['Int']>;
  take?: Maybe<Scalars['Int']>;
  where?: Maybe<ITenantFilterInput>;
}>;


export type TenantsQuery = (
  { __typename?: 'Query' }
  & { tenants?: Maybe<(
    { __typename?: 'TenantsCollectionSegment' }
    & Pick<TenantsCollectionSegment, 'totalCount'>
    & { items?: Maybe<Array<(
      { __typename?: 'Tenant' }
      & Pick<Tenant, 'code' | 'name' | 'isEnabled' | 'id'>
    )>>, pageInfo: (
      { __typename?: 'CollectionSegmentInfo' }
      & PageInfoFragment
    ) }
  )> }
);

export type ToggleTenantAvailabilityMutationVariables = Exact<{
  code: Scalars['String'];
}>;


export type ToggleTenantAvailabilityMutation = (
  { __typename?: 'Mutation' }
  & Pick<Mutation, 'toggleTenantAvailability'>
);

export type EditTenantMutationVariables = Exact<{
  code: Scalars['String'];
  name: Scalars['String'];
}>;


export type EditTenantMutation = (
  { __typename?: 'Mutation' }
  & Pick<Mutation, 'editTenant'>
);

export type CreateTenantMutationVariables = Exact<{
  code: Scalars['String'];
  name: Scalars['String'];
}>;


export type CreateTenantMutation = (
  { __typename?: 'Mutation' }
  & { createTenant: (
    { __typename?: 'Tenant' }
    & Pick<Tenant, 'code' | 'isEnabled' | 'name' | 'id'>
  ) }
);

export type SettingBriefFragment = (
  { __typename?: 'Setting' }
  & Pick<Setting, 'id' | 'name' | 'value'>
);

export type SettingDetailFragment = (
  { __typename?: 'Setting' }
  & Pick<Setting, 'scope' | 'scopedKey'>
);

export type EditSettingMutationVariables = Exact<{
  input?: Maybe<EditSettingRequestInput>;
}>;


export type EditSettingMutation = (
  { __typename?: 'Mutation' }
  & { editSetting?: Maybe<(
    { __typename?: 'Setting' }
    & Pick<Setting, 'name' | 'value'>
  )> }
);

export type SettingsQueryVariables = Exact<{
  input: GetSettingsInput;
  skip?: Maybe<Scalars['Int']>;
  take?: Maybe<Scalars['Int']>;
  where?: Maybe<ISettingFilterInput>;
  includeDetail: Scalars['Boolean'];
}>;


export type SettingsQuery = (
  { __typename?: 'Query' }
  & { settings?: Maybe<(
    { __typename?: 'SettingsCollectionSegment' }
    & Pick<SettingsCollectionSegment, 'totalCount'>
    & { items?: Maybe<Array<Maybe<(
      { __typename?: 'Setting' }
      & SettingBriefFragment
      & SettingDetailFragment
    )>>>, pageInfo: (
      { __typename?: 'CollectionSegmentInfo' }
      & PageInfoFragment
    ) }
  )> }
);

export type InitSettingsQueryVariables = Exact<{ [key: string]: never; }>;


export type InitSettingsQuery = (
  { __typename?: 'Query' }
  & { initSettings?: Maybe<Array<Maybe<(
    { __typename?: 'Setting' }
    & Pick<Setting, 'name' | 'value'>
  )>>> }
);

export type PageInfoFragment = (
  { __typename?: 'CollectionSegmentInfo' }
  & Pick<CollectionSegmentInfo, 'hasPreviousPage' | 'hasNextPage'>
);

export type BlobObjectBriefFragment = (
  { __typename?: 'BlobObject' }
  & Pick<BlobObject, 'id' | 'createdOn' | 'fileSize' | 'mimeType' | 'storageType' | 'fileName' | 'md5' | 'url'>
);

export type BlobObjectDetailFragment = (
  { __typename?: 'BlobObject' }
  & Pick<BlobObject, 'url'>
);

export type CreateBlobObjectMutationVariables = Exact<{
  input?: Maybe<CreateBlobObjectRequestInput>;
}>;


export type CreateBlobObjectMutation = (
  { __typename?: 'Mutation' }
  & { createBlobObject?: Maybe<(
    { __typename?: 'BlobObject' }
    & Pick<BlobObject, 'id' | 'md5' | 'fileName' | 'url' | 'mimeType' | 'fileSize' | 'storageType'>
  )> }
);

export type CheckTenantMutationVariables = Exact<{
  code: Scalars['String'];
}>;


export type CheckTenantMutation = (
  { __typename?: 'Mutation' }
  & { checkTenant?: Maybe<(
    { __typename?: 'Tenant' }
    & Pick<Tenant, 'code' | 'name' | 'isEnabled' | 'id' | 'createdOn'>
  )> }
);

export type BlobObjectsQueryVariables = Exact<{
  skip?: Maybe<Scalars['Int']>;
  take?: Maybe<Scalars['Int']>;
  where?: Maybe<IBlobObjectFilterInput>;
  includeDetail: Scalars['Boolean'];
}>;


export type BlobObjectsQuery = (
  { __typename?: 'Query' }
  & { blobObjects?: Maybe<(
    { __typename?: 'BlobObjectsCollectionSegment' }
    & Pick<BlobObjectsCollectionSegment, 'totalCount'>
    & { items?: Maybe<Array<Maybe<(
      { __typename?: 'BlobObject' }
      & BlobObjectBriefFragment
      & BlobObjectDetailFragment
    )>>>, pageInfo: (
      { __typename?: 'CollectionSegmentInfo' }
      & PageInfoFragment
    ) }
  )> }
);

export type OrgCacheItemFragment = (
  { __typename?: 'OrgCacheItem' }
  & Pick<OrgCacheItem, 'orgType' | 'code' | 'name' | 'parentOrgCode'>
);

export type OrgsCacheQueryVariables = Exact<{ [key: string]: never; }>;


export type OrgsCacheQuery = (
  { __typename?: 'Query' }
  & { orgsCache: Array<(
    { __typename?: 'OrgCacheItem' }
    & OrgCacheItemFragment
  )> }
);

export type OnFrontendCallSubscriptionVariables = Exact<{ [key: string]: never; }>;


export type OnFrontendCallSubscription = (
  { __typename?: 'Subscription' }
  & { onFrontendCall: (
    { __typename?: 'FrontendCall' }
    & Pick<FrontendCall, 'frontendCallType' | 'data'>
  ) }
);

export type OnBroadcastSubscriptionVariables = Exact<{ [key: string]: never; }>;


export type OnBroadcastSubscription = (
  { __typename?: 'Subscription' }
  & { onBroadcast: (
    { __typename?: 'FrontendCall' }
    & Pick<FrontendCall, 'frontendCallType' | 'data'>
  ) }
);

export const BookBriefGql = gql`
    fragment BookBrief on Book {
  id
  name
  auditStatus
  createdOn
}
    ` as unknown as DocumentNode<BookBriefFragment, unknown>;
export const BookDetailGql = gql`
    fragment BookDetail on Book {
  id
  name
  auditStatus
  createdOn
}
    ` as unknown as DocumentNode<BookDetailFragment, unknown>;
export const RoleBriefGql = gql`
    fragment RoleBrief on Role {
  createdOn
  name
  id
  isStatic
  isDefault
}
    ` as unknown as DocumentNode<RoleBriefFragment, unknown>;
export const RoleDetailGql = gql`
    fragment RoleDetail on Role {
  ...RoleBrief
  permissions
  name
  users {
    id
  }
}
    ${RoleBriefGql}` as unknown as DocumentNode<RoleDetailFragment, unknown>;
export const RoleMinimalGql = gql`
    fragment RoleMinimal on Role {
  id
  name
}
    ` as unknown as DocumentNode<RoleMinimalFragment, unknown>;
export const OrgBriefGql = gql`
    fragment OrgBrief on Org {
  code
  name
  orgType
  parentOrgCode
  id
}
    ` as unknown as DocumentNode<OrgBriefFragment, unknown>;
export const OrgDetailGql = gql`
    fragment OrgDetail on Org {
  ...OrgBrief
  allSubOrgs {
    name
    code
  }
  directSubOrgs {
    name
    code
  }
}
    ${OrgBriefGql}` as unknown as DocumentNode<OrgDetailFragment, unknown>;
export const UserBriefGql = gql`
    fragment UserBrief on User {
  id
  username
  nickname
  phoneNumber
  email
  isEnable
  openId
  loginProvider
  roleNames
  roleIds
}
    ` as unknown as DocumentNode<UserBriefFragment, unknown>;
export const UserListGql = gql`
    fragment UserList on User {
  ...UserBrief
  createdOn
  orgCodes
}
    ${UserBriefGql}` as unknown as DocumentNode<UserListFragment, unknown>;
export const UserCacheDtoGql = gql`
    fragment UserCacheDto on User {
  ...UserBrief
  avatarFile {
    url
  }
}
    ${UserBriefGql}` as unknown as DocumentNode<UserCacheDtoFragment, unknown>;
export const UserMinimalGql = gql`
    fragment UserMinimal on User {
  id
  openId
  username
  nickname
}
    ` as unknown as DocumentNode<UserMinimalFragment, unknown>;
export const OrgRecursiveParentGql = gql`
    fragment OrgRecursiveParent on Org {
  parentOrg {
    ...OrgBrief
    parentOrg {
      ...OrgBrief
      parentOrg {
        ...OrgBrief
        parentOrg {
          ...OrgBrief
          parentOrg {
            ...OrgBrief
            parentOrg {
              ...OrgBrief
              parentOrg {
                ...OrgBrief
                parentOrg {
                  ...OrgBrief
                  parentOrg {
                    ...OrgBrief
                    parentOrg {
                      ...OrgBrief
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
  }
}
    ${OrgBriefGql}` as unknown as DocumentNode<OrgRecursiveParentFragment, unknown>;
export const MessageBriefGql = gql`
    fragment MessageBrief on Message {
  fromUserId
  id
  messageType
  severity
  time
  title
}
    ` as unknown as DocumentNode<MessageBriefFragment, unknown>;
export const MessageDetailGql = gql`
    fragment MessageDetail on Message {
  toUserIds
  createdOn
  content {
    _
  }
}
    ` as unknown as DocumentNode<MessageDetailFragment, unknown>;
export const BlobObjectBriefGql = gql`
    fragment BlobObjectBrief on BlobObject {
  id
  createdOn
  fileSize
  mimeType
  storageType
  fileName
  md5
  url
}
    ` as unknown as DocumentNode<BlobObjectBriefFragment, unknown>;
export const UserDetailGql = gql`
    fragment UserDetail on User {
  ...UserBrief
  isEnable
  permissions
  avatarFile {
    url
    ...BlobObjectBrief
  }
  orgs {
    allParentOrgs {
      code
      name
    }
    name
    code
  }
  claims {
    claimType
    claimValue
  }
  orgCodes
  avatarFileId
  avatarFile {
    ...BlobObjectBrief
  }
}
    ${UserBriefGql}
${BlobObjectBriefGql}` as unknown as DocumentNode<UserDetailFragment, unknown>;
export const AuthenticateResultFragmentGql = gql`
    fragment AuthenticateResultFragment on UserToken {
  userId
  user {
    ...UserDetail
  }
  token
}
    ${UserDetailGql}` as unknown as DocumentNode<AuthenticateResultFragmentFragment, unknown>;
export const SettingBriefGql = gql`
    fragment SettingBrief on Setting {
  id
  name
  value
}
    ` as unknown as DocumentNode<SettingBriefFragment, unknown>;
export const SettingDetailGql = gql`
    fragment SettingDetail on Setting {
  scope
  scopedKey
}
    ` as unknown as DocumentNode<SettingDetailFragment, unknown>;
export const PageInfoGql = gql`
    fragment PageInfo on CollectionSegmentInfo {
  hasPreviousPage
  hasNextPage
}
    ` as unknown as DocumentNode<PageInfoFragment, unknown>;
export const BlobObjectDetailGql = gql`
    fragment BlobObjectDetail on BlobObject {
  url
}
    ` as unknown as DocumentNode<BlobObjectDetailFragment, unknown>;
export const OrgCacheItemGql = gql`
    fragment OrgCacheItem on OrgCacheItem {
  orgType
  code
  name
  parentOrgCode
}
    ` as unknown as DocumentNode<OrgCacheItemFragment, unknown>;
export const BooksGql = gql`
    query books($input: QueryBookInput!, $skip: Int, $take: Int, $where: BookFilterInput, $order: [BookSortInput!] = [{createdOn: DESC}]) {
  books(input: $input, skip: $skip, take: $take, where: $where, order: $order) {
    items {
      ...BookBrief
    }
    pageInfo {
      ...PageInfo
    }
    totalCount
  }
}
    ${BookBriefGql}
${PageInfoGql}` as unknown as DocumentNode<BooksQuery, BooksQueryVariables>;
export const BookByIdGql = gql`
    query bookById($id: String!) {
  bookById(id: $id) {
    ...BookDetail
  }
}
    ${BookDetailGql}` as unknown as DocumentNode<BookByIdQuery, BookByIdQueryVariables>;
export const CreateBooksGql = gql`
    mutation createBooks($input: CreateBookInput!) {
  createBook(input: $input) {
    id
  }
}
    ` as unknown as DocumentNode<CreateBooksMutation, CreateBooksMutationVariables>;
export const DeleteBooksGql = gql`
    mutation deleteBooks($ids: [String!]!) {
  deleteBook(ids: $ids)
}
    ` as unknown as DocumentNode<DeleteBooksMutation, DeleteBooksMutationVariables>;
export const EditBooksGql = gql`
    mutation editBooks($id: String!, $input: EditBookInput!) {
  editBook(id: $id, input: $input)
}
    ` as unknown as DocumentNode<EditBooksMutation, EditBooksMutationVariables>;
export const AuditBookGql = gql`
    mutation auditBook($ids: [String]) {
  auditBook(ids: $ids)
}
    ` as unknown as DocumentNode<AuditBookMutation, AuditBookMutationVariables>;
export const UnauditBookGql = gql`
    mutation unauditBook($ids: [String]) {
  unauditBook(ids: $ids)
}
    ` as unknown as DocumentNode<UnauditBookMutation, UnauditBookMutationVariables>;
export const SubmitBooksGql = gql`
    mutation submitBooks($ids: [String]) {
  submitBook(ids: $ids)
}
    ` as unknown as DocumentNode<SubmitBooksMutation, SubmitBooksMutationVariables>;
export const UnsubmitBooksGql = gql`
    mutation unsubmitBooks($ids: [String]) {
  unsubmitBook(ids: $ids)
}
    ` as unknown as DocumentNode<UnsubmitBooksMutation, UnsubmitBooksMutationVariables>;
export const UserListsGql = gql`
    query userLists($skip: Int, $take: Int, $where: IUserFilterInput) {
  users(skip: $skip, take: $take, where: $where) {
    items {
      ...UserList
    }
    pageInfo {
      ...PageInfo
    }
    totalCount
  }
}
    ${UserListGql}
${PageInfoGql}` as unknown as DocumentNode<UserListsQuery, UserListsQueryVariables>;
export const UserByIdGql = gql`
    query userById($id: String) {
  users(skip: 0, take: 1, where: {id: {eq: $id}}) {
    items {
      ...UserDetail
    }
  }
}
    ${UserDetailGql}` as unknown as DocumentNode<UserByIdQuery, UserByIdQueryVariables>;
export const UserMenusGql = gql`
    query userMenus($where: IUserFilterInput) {
  users(skip: 0, take: 999, where: $where) {
    items {
      ...UserMinimal
    }
  }
}
    ${UserMinimalGql}` as unknown as DocumentNode<UserMenusQuery, UserMenusQueryVariables>;
export const EditUserGql = gql`
    mutation editUser($input: EditUserRequestInput!) {
  editUser(input: $input)
}
    ` as unknown as DocumentNode<EditUserMutation, EditUserMutationVariables>;
export const CreateUserGql = gql`
    mutation createUser($input: CreateUserRequestInput!) {
  createUser(input: $input)
}
    ` as unknown as DocumentNode<CreateUserMutation, CreateUserMutationVariables>;
export const ResetUserPasswordGql = gql`
    mutation resetUserPassword($input: ResetUserPasswordRequestInput!) {
  resetUserPassword(input: $input)
}
    ` as unknown as DocumentNode<ResetUserPasswordMutation, ResetUserPasswordMutationVariables>;
export const ChangePasswordGql = gql`
    mutation changePassword($input: ChangePasswordRequestInput!) {
  changePassword(input: $input)
}
    ` as unknown as DocumentNode<ChangePasswordMutation, ChangePasswordMutationVariables>;
export const RoleListsGql = gql`
    query roleLists($skip: Int, $take: Int, $where: RoleFilterInput) {
  roles(skip: $skip, take: $take, where: $where) {
    items {
      ...RoleBrief
    }
    pageInfo {
      ...PageInfo
    }
    totalCount
  }
}
    ${RoleBriefGql}
${PageInfoGql}` as unknown as DocumentNode<RoleListsQuery, RoleListsQueryVariables>;
export const RoleMenusGql = gql`
    query roleMenus($where: RoleFilterInput) {
  roles(skip: 0, take: 999, where: $where) {
    items {
      ...RoleMinimal
    }
  }
}
    ${RoleMinimalGql}` as unknown as DocumentNode<RoleMenusQuery, RoleMenusQueryVariables>;
export const RoleByNameGql = gql`
    query roleByName($name: String) {
  roles(skip: 0, take: 1, where: {name: {eq: $name}}) {
    items {
      ...RoleDetail
    }
  }
}
    ${RoleDetailGql}` as unknown as DocumentNode<RoleByNameQuery, RoleByNameQueryVariables>;
export const RoleByIdGql = gql`
    query roleById($id: String) {
  roles(skip: 0, take: 1, where: {id: {eq: $id}}) {
    items {
      ...RoleDetail
    }
  }
}
    ${RoleDetailGql}` as unknown as DocumentNode<RoleByIdQuery, RoleByIdQueryVariables>;
export const CreateRoleGql = gql`
    mutation createRole($input: CreateRoleInput!) {
  createRole(input: $input) {
    id
    createdOn
    name
    users {
      id
      username
      email
      phoneNumber
      ... on User {
        permissions
      }
    }
    permissions
  }
}
    ` as unknown as DocumentNode<CreateRoleMutation, CreateRoleMutationVariables>;
export const AuthorizeGql = gql`
    mutation authorize($input: AuthorizeInput!) {
  authorize(input: $input)
}
    ` as unknown as DocumentNode<AuthorizeMutation, AuthorizeMutationVariables>;
export const OrgsGql = gql`
    query orgs($where: OrgFilterInput) {
  orgs(skip: 0, take: 999, where: $where) {
    items {
      ...OrgBrief
    }
  }
}
    ${OrgBriefGql}` as unknown as DocumentNode<OrgsQuery, OrgsQueryVariables>;
export const CreateOrgGql = gql`
    mutation createOrg($input: CreateOrgInput!) {
  createOrg(input: $input) {
    ...OrgBrief
  }
}
    ${OrgBriefGql}` as unknown as DocumentNode<CreateOrgMutation, CreateOrgMutationVariables>;
export const AssignOrgsGql = gql`
    mutation assignOrgs($input: AssignOrgRequestInput!) {
  assignOrgs(input: $input)
}
    ` as unknown as DocumentNode<AssignOrgsMutation, AssignOrgsMutationVariables>;
export const SetRoleDefaultGql = gql`
    mutation setRoleDefault($roleId: String!) {
  setRoleDefault(input: {roleId: $roleId})
}
    ` as unknown as DocumentNode<SetRoleDefaultMutation, SetRoleDefaultMutationVariables>;
export const CreateMessageGql = gql`
    mutation createMessage($input: CreateMessageRequestInput!) {
  createMessage(input: $input) {
    ... on Message {
      id
    }
  }
}
    ` as unknown as DocumentNode<CreateMessageMutation, CreateMessageMutationVariables>;
export const EditMessageGql = gql`
    mutation editMessage($input: EditMessageRequestInput!) {
  editMessage(input: $input)
}
    ` as unknown as DocumentNode<EditMessageMutation, EditMessageMutationVariables>;
export const SendMessageGql = gql`
    mutation sendMessage($input: SendNotificationMessageRequestInput!) {
  sendMessage(input: $input)
}
    ` as unknown as DocumentNode<SendMessageMutation, SendMessageMutationVariables>;
export const MessagesGql = gql`
    query messages($skip: Int, $take: Int, $where: IMessageFilterInput, $includeDetail: Boolean!) {
  messages(skip: $skip, take: $take, where: $where) {
    items {
      ...MessageBrief
      ...MessageDetail @include(if: $includeDetail)
    }
    pageInfo {
      ...PageInfo
    }
    totalCount
  }
}
    ${MessageBriefGql}
${MessageDetailGql}
${PageInfoGql}` as unknown as DocumentNode<MessagesQuery, MessagesQueryVariables>;
export const AuthenticateGql = gql`
    mutation authenticate($input: AuthenticateInput!) {
  authenticate(input: $input) {
    ...AuthenticateResultFragment
  }
}
    ${AuthenticateResultFragmentGql}` as unknown as DocumentNode<AuthenticateMutation, AuthenticateMutationVariables>;
export const FederateAuthenticateGql = gql`
    mutation federateAuthenticate($code: String!, $loginProvider: LoginProviderEnum!) {
  federateAuthenticate(input: {code: $code, loginProvider: $loginProvider}) {
    ...AuthenticateResultFragment
  }
}
    ${AuthenticateResultFragmentGql}` as unknown as DocumentNode<FederateAuthenticateMutation, FederateAuthenticateMutationVariables>;
export const RegisterAndSignInGql = gql`
    mutation registerAndSignIn($registerInput: RegisterUserRequestInput!, $authenticateInput: AuthenticateInput!) {
  register(input: $registerInput)
  authenticate(input: $authenticateInput) {
    userId
    user {
      id
      ... on User {
        roleNames
        roleIds
        permissions
        avatarFile {
          url
        }
      }
      phoneNumber
      email
      username
    }
    token
  }
}
    ` as unknown as DocumentNode<RegisterAndSignInMutation, RegisterAndSignInMutationVariables>;
export const SendSmsCaptchaGql = gql`
    mutation sendSmsCaptcha($phoneOrEmail: ChinesePhoneNumberType!) {
  generateCaptcha(
    input: {captchaProvider: Sms, smsCaptchaPhoneNumber: $phoneOrEmail}
  ) {
    captchaType
    key
  }
}
    ` as unknown as DocumentNode<SendSmsCaptchaMutation, SendSmsCaptchaMutationVariables>;
export const ValidateSmsCaptchaGql = gql`
    mutation validateSmsCaptcha($captchaKey: String!, $captchaCode: String!) {
  validateCaptcha(
    input: {captchaProvider: Sms, captchaKey: $captchaKey, captchaCode: $captchaCode}
  )
}
    ` as unknown as DocumentNode<ValidateSmsCaptchaMutation, ValidateSmsCaptchaMutationVariables>;
export const TenantsGql = gql`
    query tenants($skip: Int, $take: Int, $where: ITenantFilterInput) {
  tenants(skip: $skip, take: $take, where: $where) {
    items {
      code
      name
      isEnabled
      id
    }
    pageInfo {
      ...PageInfo
    }
    totalCount
  }
}
    ${PageInfoGql}` as unknown as DocumentNode<TenantsQuery, TenantsQueryVariables>;
export const ToggleTenantAvailabilityGql = gql`
    mutation toggleTenantAvailability($code: String!) {
  toggleTenantAvailability(input: {code: $code})
}
    ` as unknown as DocumentNode<ToggleTenantAvailabilityMutation, ToggleTenantAvailabilityMutationVariables>;
export const EditTenantGql = gql`
    mutation editTenant($code: String!, $name: String!) {
  editTenant(input: {code: $code, name: $name})
}
    ` as unknown as DocumentNode<EditTenantMutation, EditTenantMutationVariables>;
export const CreateTenantGql = gql`
    mutation createTenant($code: String!, $name: String!) {
  createTenant(input: {code: $code, name: $name}) {
    code
    isEnabled
    name
    id
  }
}
    ` as unknown as DocumentNode<CreateTenantMutation, CreateTenantMutationVariables>;
export const EditSettingGql = gql`
    mutation editSetting($input: EditSettingRequestInput) {
  editSetting(input: $input) {
    name
    value
  }
}
    ` as unknown as DocumentNode<EditSettingMutation, EditSettingMutationVariables>;
export const SettingsGql = gql`
    query settings($input: GetSettingsInput!, $skip: Int, $take: Int, $where: ISettingFilterInput, $includeDetail: Boolean!) {
  settings(input: $input, skip: $skip, take: $take, where: $where) {
    items {
      ...SettingBrief
      ...SettingDetail @include(if: $includeDetail)
    }
    pageInfo {
      ...PageInfo
    }
    totalCount
  }
}
    ${SettingBriefGql}
${SettingDetailGql}
${PageInfoGql}` as unknown as DocumentNode<SettingsQuery, SettingsQueryVariables>;
export const InitSettingsGql = gql`
    query initSettings {
  initSettings {
    name
    value
  }
}
    ` as unknown as DocumentNode<InitSettingsQuery, InitSettingsQueryVariables>;
export const CreateBlobObjectGql = gql`
    mutation createBlobObject($input: CreateBlobObjectRequestInput) {
  createBlobObject(input: $input) {
    id
    md5
    fileName
    url
    mimeType
    fileSize
    storageType
  }
}
    ` as unknown as DocumentNode<CreateBlobObjectMutation, CreateBlobObjectMutationVariables>;
export const CheckTenantGql = gql`
    mutation checkTenant($code: String!) {
  checkTenant(code: $code) {
    code
    name
    isEnabled
    id
    createdOn
  }
}
    ` as unknown as DocumentNode<CheckTenantMutation, CheckTenantMutationVariables>;
export const BlobObjectsGql = gql`
    query blobObjects($skip: Int, $take: Int, $where: IBlobObjectFilterInput, $includeDetail: Boolean!) {
  blobObjects(skip: $skip, take: $take, where: $where) {
    items {
      ...BlobObjectBrief
      ...BlobObjectDetail @include(if: $includeDetail)
    }
    pageInfo {
      ...PageInfo
    }
    totalCount
  }
}
    ${BlobObjectBriefGql}
${BlobObjectDetailGql}
${PageInfoGql}` as unknown as DocumentNode<BlobObjectsQuery, BlobObjectsQueryVariables>;
export const OrgsCacheGql = gql`
    query orgsCache {
  orgsCache {
    ...OrgCacheItem
  }
}
    ${OrgCacheItemGql}` as unknown as DocumentNode<OrgsCacheQuery, OrgsCacheQueryVariables>;
export const OnFrontendCallGql = gql`
    subscription onFrontendCall {
  onFrontendCall {
    frontendCallType
    data
  }
}
    ` as unknown as DocumentNode<OnFrontendCallSubscription, OnFrontendCallSubscriptionVariables>;
export const OnBroadcastGql = gql`
    subscription onBroadcast {
  onBroadcast {
    frontendCallType
    data
  }
}
    ` as unknown as DocumentNode<OnBroadcastSubscription, OnBroadcastSubscriptionVariables>;