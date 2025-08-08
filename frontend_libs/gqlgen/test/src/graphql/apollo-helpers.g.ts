import { FieldPolicy, FieldReadFunction, TypePolicies, TypePolicy } from '@apollo/client/cache';
export type AuditLogKeySpecifier = ('clientIp' | 'createdOn' | 'id' | 'isSuccess' | 'modifiedOn' | 'operation' | 'operationName' | 'operationType' | 'operatorId' | 'result' | 'tenantCode' | 'variables' | AuditLogKeySpecifier)[];
export type AuditLogFieldPolicy = {
	clientIp?: FieldPolicy<any> | FieldReadFunction<any>,
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>,
	id?: FieldPolicy<any> | FieldReadFunction<any>,
	isSuccess?: FieldPolicy<any> | FieldReadFunction<any>,
	modifiedOn?: FieldPolicy<any> | FieldReadFunction<any>,
	operation?: FieldPolicy<any> | FieldReadFunction<any>,
	operationName?: FieldPolicy<any> | FieldReadFunction<any>,
	operationType?: FieldPolicy<any> | FieldReadFunction<any>,
	operatorId?: FieldPolicy<any> | FieldReadFunction<any>,
	result?: FieldPolicy<any> | FieldReadFunction<any>,
	tenantCode?: FieldPolicy<any> | FieldReadFunction<any>,
	variables?: FieldPolicy<any> | FieldReadFunction<any>
};
export type BlobObjectKeySpecifier = ('createdOn' | 'expireAt' | 'fileName' | 'fileSize' | 'id' | 'md5' | 'mimeType' | 'modifiedOn' | 'storageType' | 'url' | BlobObjectKeySpecifier)[];
export type BlobObjectFieldPolicy = {
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>,
	expireAt?: FieldPolicy<any> | FieldReadFunction<any>,
	fileName?: FieldPolicy<any> | FieldReadFunction<any>,
	fileSize?: FieldPolicy<any> | FieldReadFunction<any>,
	id?: FieldPolicy<any> | FieldReadFunction<any>,
	md5?: FieldPolicy<any> | FieldReadFunction<any>,
	mimeType?: FieldPolicy<any> | FieldReadFunction<any>,
	modifiedOn?: FieldPolicy<any> | FieldReadFunction<any>,
	storageType?: FieldPolicy<any> | FieldReadFunction<any>,
	url?: FieldPolicy<any> | FieldReadFunction<any>
};
export type BlobObjectsCollectionSegmentKeySpecifier = ('items' | 'pageInfo' | 'totalCount' | BlobObjectsCollectionSegmentKeySpecifier)[];
export type BlobObjectsCollectionSegmentFieldPolicy = {
	items?: FieldPolicy<any> | FieldReadFunction<any>,
	pageInfo?: FieldPolicy<any> | FieldReadFunction<any>,
	totalCount?: FieldPolicy<any> | FieldReadFunction<any>
};
export type CaptchaKeySpecifier = ('bitmap' | 'captchaType' | 'key' | CaptchaKeySpecifier)[];
export type CaptchaFieldPolicy = {
	bitmap?: FieldPolicy<any> | FieldReadFunction<any>,
	captchaType?: FieldPolicy<any> | FieldReadFunction<any>,
	key?: FieldPolicy<any> | FieldReadFunction<any>
};
export type ClientNotifyKeySpecifier = ('createdOn' | ClientNotifyKeySpecifier)[];
export type ClientNotifyFieldPolicy = {
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>
};
export type CollectionSegmentInfoKeySpecifier = ('hasNextPage' | 'hasPreviousPage' | CollectionSegmentInfoKeySpecifier)[];
export type CollectionSegmentInfoFieldPolicy = {
	hasNextPage?: FieldPolicy<any> | FieldReadFunction<any>,
	hasPreviousPage?: FieldPolicy<any> | FieldReadFunction<any>
};
export type DataChangeClientNotifyKeySpecifier = ('createdOn' | 'dataChangeType' | DataChangeClientNotifyKeySpecifier)[];
export type DataChangeClientNotifyFieldPolicy = {
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>,
	dataChangeType?: FieldPolicy<any> | FieldReadFunction<any>
};
export type DeviceAuthorizationStatusResultKeySpecifier = ('deviceId' | 'deviceName' | 'squadStatuses' | 'userId' | DeviceAuthorizationStatusResultKeySpecifier)[];
export type DeviceAuthorizationStatusResultFieldPolicy = {
	deviceId?: FieldPolicy<any> | FieldReadFunction<any>,
	deviceName?: FieldPolicy<any> | FieldReadFunction<any>,
	squadStatuses?: FieldPolicy<any> | FieldReadFunction<any>,
	userId?: FieldPolicy<any> | FieldReadFunction<any>
};
export type ExecutionHistoriesCollectionSegmentKeySpecifier = ('items' | 'pageInfo' | 'totalCount' | ExecutionHistoriesCollectionSegmentKeySpecifier)[];
export type ExecutionHistoriesCollectionSegmentFieldPolicy = {
	items?: FieldPolicy<any> | FieldReadFunction<any>,
	pageInfo?: FieldPolicy<any> | FieldReadFunction<any>,
	totalCount?: FieldPolicy<any> | FieldReadFunction<any>
};
export type IAuthUserKeySpecifier = ('createdOn' | 'email' | 'id' | 'isEnable' | 'loginProvider' | 'nickname' | 'openId' | 'phoneNumber' | 'username' | IAuthUserKeySpecifier)[];
export type IAuthUserFieldPolicy = {
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>,
	email?: FieldPolicy<any> | FieldReadFunction<any>,
	id?: FieldPolicy<any> | FieldReadFunction<any>,
	isEnable?: FieldPolicy<any> | FieldReadFunction<any>,
	loginProvider?: FieldPolicy<any> | FieldReadFunction<any>,
	nickname?: FieldPolicy<any> | FieldReadFunction<any>,
	openId?: FieldPolicy<any> | FieldReadFunction<any>,
	phoneNumber?: FieldPolicy<any> | FieldReadFunction<any>,
	username?: FieldPolicy<any> | FieldReadFunction<any>
};
export type IBlobObjectKeySpecifier = ('createdOn' | 'expireAt' | 'fileName' | 'fileSize' | 'id' | 'md5' | 'mimeType' | 'storageType' | 'url' | IBlobObjectKeySpecifier)[];
export type IBlobObjectFieldPolicy = {
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>,
	expireAt?: FieldPolicy<any> | FieldReadFunction<any>,
	fileName?: FieldPolicy<any> | FieldReadFunction<any>,
	fileSize?: FieldPolicy<any> | FieldReadFunction<any>,
	id?: FieldPolicy<any> | FieldReadFunction<any>,
	md5?: FieldPolicy<any> | FieldReadFunction<any>,
	mimeType?: FieldPolicy<any> | FieldReadFunction<any>,
	storageType?: FieldPolicy<any> | FieldReadFunction<any>,
	url?: FieldPolicy<any> | FieldReadFunction<any>
};
export type IEntityBaseKeySpecifier = ('createdOn' | 'id' | IEntityBaseKeySpecifier)[];
export type IEntityBaseFieldPolicy = {
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>,
	id?: FieldPolicy<any> | FieldReadFunction<any>
};
export type IMessageKeySpecifier = ('content' | 'createdOn' | 'fromUserId' | 'id' | 'messageType' | 'severity' | 'time' | 'title' | 'toUserIds' | IMessageKeySpecifier)[];
export type IMessageFieldPolicy = {
	content?: FieldPolicy<any> | FieldReadFunction<any>,
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>,
	fromUserId?: FieldPolicy<any> | FieldReadFunction<any>,
	id?: FieldPolicy<any> | FieldReadFunction<any>,
	messageType?: FieldPolicy<any> | FieldReadFunction<any>,
	severity?: FieldPolicy<any> | FieldReadFunction<any>,
	time?: FieldPolicy<any> | FieldReadFunction<any>,
	title?: FieldPolicy<any> | FieldReadFunction<any>,
	toUserIds?: FieldPolicy<any> | FieldReadFunction<any>
};
export type IMessageContentKeySpecifier = ('_' | IMessageContentKeySpecifier)[];
export type IMessageContentFieldPolicy = {
	_?: FieldPolicy<any> | FieldReadFunction<any>
};
export type IOrgKeySpecifier = ('allParentOrgCodes' | 'allParentOrgs' | 'allSubOrgCodes' | 'allSubOrgs' | 'code' | 'createdOn' | 'directSubOrgCodes' | 'directSubOrgs' | 'id' | 'name' | 'orgType' | 'parentOrg' | 'parentOrgCode' | IOrgKeySpecifier)[];
export type IOrgFieldPolicy = {
	allParentOrgCodes?: FieldPolicy<any> | FieldReadFunction<any>,
	allParentOrgs?: FieldPolicy<any> | FieldReadFunction<any>,
	allSubOrgCodes?: FieldPolicy<any> | FieldReadFunction<any>,
	allSubOrgs?: FieldPolicy<any> | FieldReadFunction<any>,
	code?: FieldPolicy<any> | FieldReadFunction<any>,
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>,
	directSubOrgCodes?: FieldPolicy<any> | FieldReadFunction<any>,
	directSubOrgs?: FieldPolicy<any> | FieldReadFunction<any>,
	id?: FieldPolicy<any> | FieldReadFunction<any>,
	name?: FieldPolicy<any> | FieldReadFunction<any>,
	orgType?: FieldPolicy<any> | FieldReadFunction<any>,
	parentOrg?: FieldPolicy<any> | FieldReadFunction<any>,
	parentOrgCode?: FieldPolicy<any> | FieldReadFunction<any>
};
export type IPagedListKeySpecifier = ('pageIndex' | 'pageSize' | 'totalCount' | 'totalPage' | IPagedListKeySpecifier)[];
export type IPagedListFieldPolicy = {
	pageIndex?: FieldPolicy<any> | FieldReadFunction<any>,
	pageSize?: FieldPolicy<any> | FieldReadFunction<any>,
	totalCount?: FieldPolicy<any> | FieldReadFunction<any>,
	totalPage?: FieldPolicy<any> | FieldReadFunction<any>
};
export type IQuicollabUserKeySpecifier = ('avatarFile' | 'avatarFileId' | 'claims' | 'createdOn' | 'devices' | 'email' | 'id' | 'isEnable' | 'loginProvider' | 'nickname' | 'openId' | 'orgCodes' | 'orgs' | 'permissions' | 'phoneNumber' | 'roleIds' | 'roleNames' | 'roles' | 'username' | IQuicollabUserKeySpecifier)[];
export type IQuicollabUserFieldPolicy = {
	avatarFile?: FieldPolicy<any> | FieldReadFunction<any>,
	avatarFileId?: FieldPolicy<any> | FieldReadFunction<any>,
	claims?: FieldPolicy<any> | FieldReadFunction<any>,
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>,
	devices?: FieldPolicy<any> | FieldReadFunction<any>,
	email?: FieldPolicy<any> | FieldReadFunction<any>,
	id?: FieldPolicy<any> | FieldReadFunction<any>,
	isEnable?: FieldPolicy<any> | FieldReadFunction<any>,
	loginProvider?: FieldPolicy<any> | FieldReadFunction<any>,
	nickname?: FieldPolicy<any> | FieldReadFunction<any>,
	openId?: FieldPolicy<any> | FieldReadFunction<any>,
	orgCodes?: FieldPolicy<any> | FieldReadFunction<any>,
	orgs?: FieldPolicy<any> | FieldReadFunction<any>,
	permissions?: FieldPolicy<any> | FieldReadFunction<any>,
	phoneNumber?: FieldPolicy<any> | FieldReadFunction<any>,
	roleIds?: FieldPolicy<any> | FieldReadFunction<any>,
	roleNames?: FieldPolicy<any> | FieldReadFunction<any>,
	roles?: FieldPolicy<any> | FieldReadFunction<any>,
	username?: FieldPolicy<any> | FieldReadFunction<any>
};
export type IRoleKeySpecifier = ('code' | 'createdOn' | 'id' | 'isDefault' | 'isEnabled' | 'isStatic' | 'name' | 'permissions' | 'users' | IRoleKeySpecifier)[];
export type IRoleFieldPolicy = {
	code?: FieldPolicy<any> | FieldReadFunction<any>,
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>,
	id?: FieldPolicy<any> | FieldReadFunction<any>,
	isDefault?: FieldPolicy<any> | FieldReadFunction<any>,
	isEnabled?: FieldPolicy<any> | FieldReadFunction<any>,
	isStatic?: FieldPolicy<any> | FieldReadFunction<any>,
	name?: FieldPolicy<any> | FieldReadFunction<any>,
	permissions?: FieldPolicy<any> | FieldReadFunction<any>,
	users?: FieldPolicy<any> | FieldReadFunction<any>
};
export type ISettingKeySpecifier = ('createdOn' | 'id' | 'name' | 'scope' | 'scopedKey' | 'value' | ISettingKeySpecifier)[];
export type ISettingFieldPolicy = {
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>,
	id?: FieldPolicy<any> | FieldReadFunction<any>,
	name?: FieldPolicy<any> | FieldReadFunction<any>,
	scope?: FieldPolicy<any> | FieldReadFunction<any>,
	scopedKey?: FieldPolicy<any> | FieldReadFunction<any>,
	value?: FieldPolicy<any> | FieldReadFunction<any>
};
export type IUserKeySpecifier = ('avatarFile' | 'avatarFileId' | 'claims' | 'createdOn' | 'email' | 'id' | 'isEnable' | 'loginProvider' | 'nickname' | 'openId' | 'orgCodes' | 'orgs' | 'permissions' | 'phoneNumber' | 'roleIds' | 'roleNames' | 'roles' | 'username' | IUserKeySpecifier)[];
export type IUserFieldPolicy = {
	avatarFile?: FieldPolicy<any> | FieldReadFunction<any>,
	avatarFileId?: FieldPolicy<any> | FieldReadFunction<any>,
	claims?: FieldPolicy<any> | FieldReadFunction<any>,
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>,
	email?: FieldPolicy<any> | FieldReadFunction<any>,
	id?: FieldPolicy<any> | FieldReadFunction<any>,
	isEnable?: FieldPolicy<any> | FieldReadFunction<any>,
	loginProvider?: FieldPolicy<any> | FieldReadFunction<any>,
	nickname?: FieldPolicy<any> | FieldReadFunction<any>,
	openId?: FieldPolicy<any> | FieldReadFunction<any>,
	orgCodes?: FieldPolicy<any> | FieldReadFunction<any>,
	orgs?: FieldPolicy<any> | FieldReadFunction<any>,
	permissions?: FieldPolicy<any> | FieldReadFunction<any>,
	phoneNumber?: FieldPolicy<any> | FieldReadFunction<any>,
	roleIds?: FieldPolicy<any> | FieldReadFunction<any>,
	roleNames?: FieldPolicy<any> | FieldReadFunction<any>,
	roles?: FieldPolicy<any> | FieldReadFunction<any>,
	username?: FieldPolicy<any> | FieldReadFunction<any>
};
export type JobExecutionHistoryKeySpecifier = ('createdOn' | 'executionEndTime' | 'executionStartTime' | 'id' | 'isSuccess' | 'jobName' | 'message' | 'modifiedOn' | JobExecutionHistoryKeySpecifier)[];
export type JobExecutionHistoryFieldPolicy = {
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>,
	executionEndTime?: FieldPolicy<any> | FieldReadFunction<any>,
	executionStartTime?: FieldPolicy<any> | FieldReadFunction<any>,
	id?: FieldPolicy<any> | FieldReadFunction<any>,
	isSuccess?: FieldPolicy<any> | FieldReadFunction<any>,
	jobName?: FieldPolicy<any> | FieldReadFunction<any>,
	message?: FieldPolicy<any> | FieldReadFunction<any>,
	modifiedOn?: FieldPolicy<any> | FieldReadFunction<any>
};
export type JobStateKeySpecifier = ('createdOn' | 'cron' | 'executionHistories' | 'id' | 'jobName' | 'lastExecutionTime' | 'modifiedOn' | 'nextExecutionTime' | JobStateKeySpecifier)[];
export type JobStateFieldPolicy = {
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>,
	cron?: FieldPolicy<any> | FieldReadFunction<any>,
	executionHistories?: FieldPolicy<any> | FieldReadFunction<any>,
	id?: FieldPolicy<any> | FieldReadFunction<any>,
	jobName?: FieldPolicy<any> | FieldReadFunction<any>,
	lastExecutionTime?: FieldPolicy<any> | FieldReadFunction<any>,
	modifiedOn?: FieldPolicy<any> | FieldReadFunction<any>,
	nextExecutionTime?: FieldPolicy<any> | FieldReadFunction<any>
};
export type JobStateCollectionSegmentKeySpecifier = ('items' | 'pageInfo' | 'totalCount' | JobStateCollectionSegmentKeySpecifier)[];
export type JobStateCollectionSegmentFieldPolicy = {
	items?: FieldPolicy<any> | FieldReadFunction<any>,
	pageInfo?: FieldPolicy<any> | FieldReadFunction<any>,
	totalCount?: FieldPolicy<any> | FieldReadFunction<any>
};
export type KeyValuePairOfStringAndObjectKeySpecifier = ('key' | KeyValuePairOfStringAndObjectKeySpecifier)[];
export type KeyValuePairOfStringAndObjectFieldPolicy = {
	key?: FieldPolicy<any> | FieldReadFunction<any>
};
export type MessageKeySpecifier = ('content' | 'createdOn' | 'distributions' | 'fromUserId' | 'id' | 'messageType' | 'modifiedOn' | 'severity' | 'tenantCode' | 'time' | 'title' | 'toUserIds' | MessageKeySpecifier)[];
export type MessageFieldPolicy = {
	content?: FieldPolicy<any> | FieldReadFunction<any>,
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>,
	distributions?: FieldPolicy<any> | FieldReadFunction<any>,
	fromUserId?: FieldPolicy<any> | FieldReadFunction<any>,
	id?: FieldPolicy<any> | FieldReadFunction<any>,
	messageType?: FieldPolicy<any> | FieldReadFunction<any>,
	modifiedOn?: FieldPolicy<any> | FieldReadFunction<any>,
	severity?: FieldPolicy<any> | FieldReadFunction<any>,
	tenantCode?: FieldPolicy<any> | FieldReadFunction<any>,
	time?: FieldPolicy<any> | FieldReadFunction<any>,
	title?: FieldPolicy<any> | FieldReadFunction<any>,
	toUserIds?: FieldPolicy<any> | FieldReadFunction<any>
};
export type MessageDistributionKeySpecifier = ('createdOn' | 'id' | 'isRead' | 'messageId' | 'modifiedOn' | 'toUserId' | MessageDistributionKeySpecifier)[];
export type MessageDistributionFieldPolicy = {
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>,
	id?: FieldPolicy<any> | FieldReadFunction<any>,
	isRead?: FieldPolicy<any> | FieldReadFunction<any>,
	messageId?: FieldPolicy<any> | FieldReadFunction<any>,
	modifiedOn?: FieldPolicy<any> | FieldReadFunction<any>,
	toUserId?: FieldPolicy<any> | FieldReadFunction<any>
};
export type MessagesCollectionSegmentKeySpecifier = ('items' | 'pageInfo' | 'totalCount' | MessagesCollectionSegmentKeySpecifier)[];
export type MessagesCollectionSegmentFieldPolicy = {
	items?: FieldPolicy<any> | FieldReadFunction<any>,
	pageInfo?: FieldPolicy<any> | FieldReadFunction<any>,
	totalCount?: FieldPolicy<any> | FieldReadFunction<any>
};
export type MutationKeySpecifier = ('_' | 'addMember' | 'assignOrgs' | 'assignRoles' | 'authenticate' | 'authorize' | 'authorizeDevice' | 'batchAuthorizeDevices' | 'batchRevokeDevices' | 'cancelAuthentication' | 'changePassword' | 'createBlobObject' | 'createMessage' | 'createOrg' | 'createRole' | 'createSquad' | 'createUser' | 'deleteBlobObject' | 'deleteMessageDistributions' | 'deleteOrg' | 'deleteSquad' | 'deleteUser' | 'editMessage' | 'editSetting' | 'editSquad' | 'editUser' | 'federateAuthenticate' | 'fixUserOrg' | 'generateCaptcha' | 'joinSquad' | 'leaveAllSquads' | 'leaveSquad' | 'markMessagesRead' | 'register' | 'registerDevice' | 'removeDeviceFromSquad' | 'removeMember' | 'removeMemberAdmin' | 'removeMyDevice' | 'removeUserDevicesFromSquad' | 'resetUserPassword' | 'revokeDevice' | 'sendMessage' | 'setMemberAsAdmin' | 'setRoleDefault' | 'setSquadInviteCode' | 'transferDeviceOwnership' | 'transferSquadOwnership' | 'updateDevice' | 'updateDeviceOnlineStatus' | 'updateSquadSettings' | 'updateUserDeviceName' | 'validateCaptcha' | MutationKeySpecifier)[];
export type MutationFieldPolicy = {
	_?: FieldPolicy<any> | FieldReadFunction<any>,
	addMember?: FieldPolicy<any> | FieldReadFunction<any>,
	assignOrgs?: FieldPolicy<any> | FieldReadFunction<any>,
	assignRoles?: FieldPolicy<any> | FieldReadFunction<any>,
	authenticate?: FieldPolicy<any> | FieldReadFunction<any>,
	authorize?: FieldPolicy<any> | FieldReadFunction<any>,
	authorizeDevice?: FieldPolicy<any> | FieldReadFunction<any>,
	batchAuthorizeDevices?: FieldPolicy<any> | FieldReadFunction<any>,
	batchRevokeDevices?: FieldPolicy<any> | FieldReadFunction<any>,
	cancelAuthentication?: FieldPolicy<any> | FieldReadFunction<any>,
	changePassword?: FieldPolicy<any> | FieldReadFunction<any>,
	createBlobObject?: FieldPolicy<any> | FieldReadFunction<any>,
	createMessage?: FieldPolicy<any> | FieldReadFunction<any>,
	createOrg?: FieldPolicy<any> | FieldReadFunction<any>,
	createRole?: FieldPolicy<any> | FieldReadFunction<any>,
	createSquad?: FieldPolicy<any> | FieldReadFunction<any>,
	createUser?: FieldPolicy<any> | FieldReadFunction<any>,
	deleteBlobObject?: FieldPolicy<any> | FieldReadFunction<any>,
	deleteMessageDistributions?: FieldPolicy<any> | FieldReadFunction<any>,
	deleteOrg?: FieldPolicy<any> | FieldReadFunction<any>,
	deleteSquad?: FieldPolicy<any> | FieldReadFunction<any>,
	deleteUser?: FieldPolicy<any> | FieldReadFunction<any>,
	editMessage?: FieldPolicy<any> | FieldReadFunction<any>,
	editSetting?: FieldPolicy<any> | FieldReadFunction<any>,
	editSquad?: FieldPolicy<any> | FieldReadFunction<any>,
	editUser?: FieldPolicy<any> | FieldReadFunction<any>,
	federateAuthenticate?: FieldPolicy<any> | FieldReadFunction<any>,
	fixUserOrg?: FieldPolicy<any> | FieldReadFunction<any>,
	generateCaptcha?: FieldPolicy<any> | FieldReadFunction<any>,
	joinSquad?: FieldPolicy<any> | FieldReadFunction<any>,
	leaveAllSquads?: FieldPolicy<any> | FieldReadFunction<any>,
	leaveSquad?: FieldPolicy<any> | FieldReadFunction<any>,
	markMessagesRead?: FieldPolicy<any> | FieldReadFunction<any>,
	register?: FieldPolicy<any> | FieldReadFunction<any>,
	registerDevice?: FieldPolicy<any> | FieldReadFunction<any>,
	removeDeviceFromSquad?: FieldPolicy<any> | FieldReadFunction<any>,
	removeMember?: FieldPolicy<any> | FieldReadFunction<any>,
	removeMemberAdmin?: FieldPolicy<any> | FieldReadFunction<any>,
	removeMyDevice?: FieldPolicy<any> | FieldReadFunction<any>,
	removeUserDevicesFromSquad?: FieldPolicy<any> | FieldReadFunction<any>,
	resetUserPassword?: FieldPolicy<any> | FieldReadFunction<any>,
	revokeDevice?: FieldPolicy<any> | FieldReadFunction<any>,
	sendMessage?: FieldPolicy<any> | FieldReadFunction<any>,
	setMemberAsAdmin?: FieldPolicy<any> | FieldReadFunction<any>,
	setRoleDefault?: FieldPolicy<any> | FieldReadFunction<any>,
	setSquadInviteCode?: FieldPolicy<any> | FieldReadFunction<any>,
	transferDeviceOwnership?: FieldPolicy<any> | FieldReadFunction<any>,
	transferSquadOwnership?: FieldPolicy<any> | FieldReadFunction<any>,
	updateDevice?: FieldPolicy<any> | FieldReadFunction<any>,
	updateDeviceOnlineStatus?: FieldPolicy<any> | FieldReadFunction<any>,
	updateSquadSettings?: FieldPolicy<any> | FieldReadFunction<any>,
	updateUserDeviceName?: FieldPolicy<any> | FieldReadFunction<any>,
	validateCaptcha?: FieldPolicy<any> | FieldReadFunction<any>
};
export type NewMessageClientNotifyKeySpecifier = ('createdOn' | 'message' | NewMessageClientNotifyKeySpecifier)[];
export type NewMessageClientNotifyFieldPolicy = {
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>,
	message?: FieldPolicy<any> | FieldReadFunction<any>
};
export type OrgKeySpecifier = ('allParentOrgCodes' | 'allParentOrgs' | 'allSubOrgCodes' | 'allSubOrgs' | 'code' | 'createdOn' | 'directSubOrgCodes' | 'directSubOrgs' | 'id' | 'modifiedOn' | 'name' | 'orgType' | 'parentOrg' | 'parentOrgCode' | 'tenantCode' | OrgKeySpecifier)[];
export type OrgFieldPolicy = {
	allParentOrgCodes?: FieldPolicy<any> | FieldReadFunction<any>,
	allParentOrgs?: FieldPolicy<any> | FieldReadFunction<any>,
	allSubOrgCodes?: FieldPolicy<any> | FieldReadFunction<any>,
	allSubOrgs?: FieldPolicy<any> | FieldReadFunction<any>,
	code?: FieldPolicy<any> | FieldReadFunction<any>,
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>,
	directSubOrgCodes?: FieldPolicy<any> | FieldReadFunction<any>,
	directSubOrgs?: FieldPolicy<any> | FieldReadFunction<any>,
	id?: FieldPolicy<any> | FieldReadFunction<any>,
	modifiedOn?: FieldPolicy<any> | FieldReadFunction<any>,
	name?: FieldPolicy<any> | FieldReadFunction<any>,
	orgType?: FieldPolicy<any> | FieldReadFunction<any>,
	parentOrg?: FieldPolicy<any> | FieldReadFunction<any>,
	parentOrgCode?: FieldPolicy<any> | FieldReadFunction<any>,
	tenantCode?: FieldPolicy<any> | FieldReadFunction<any>
};
export type OrgsCollectionSegmentKeySpecifier = ('items' | 'pageInfo' | 'totalCount' | OrgsCollectionSegmentKeySpecifier)[];
export type OrgsCollectionSegmentFieldPolicy = {
	items?: FieldPolicy<any> | FieldReadFunction<any>,
	pageInfo?: FieldPolicy<any> | FieldReadFunction<any>,
	totalCount?: FieldPolicy<any> | FieldReadFunction<any>
};
export type QueryKeySpecifier = ('_' | 'blobObjects' | 'currentUser' | 'deviceAuthorizationStatus' | 'deviceById' | 'initSettings' | 'jobState' | 'messages' | 'myDevices' | 'myPermissions' | 'mySquad' | 'orgs' | 'roles' | 'settings' | 'squad' | 'squadById' | 'squadInviteCode' | 'unreadMessages' | 'users' | QueryKeySpecifier)[];
export type QueryFieldPolicy = {
	_?: FieldPolicy<any> | FieldReadFunction<any>,
	blobObjects?: FieldPolicy<any> | FieldReadFunction<any>,
	currentUser?: FieldPolicy<any> | FieldReadFunction<any>,
	deviceAuthorizationStatus?: FieldPolicy<any> | FieldReadFunction<any>,
	deviceById?: FieldPolicy<any> | FieldReadFunction<any>,
	initSettings?: FieldPolicy<any> | FieldReadFunction<any>,
	jobState?: FieldPolicy<any> | FieldReadFunction<any>,
	messages?: FieldPolicy<any> | FieldReadFunction<any>,
	myDevices?: FieldPolicy<any> | FieldReadFunction<any>,
	myPermissions?: FieldPolicy<any> | FieldReadFunction<any>,
	mySquad?: FieldPolicy<any> | FieldReadFunction<any>,
	orgs?: FieldPolicy<any> | FieldReadFunction<any>,
	roles?: FieldPolicy<any> | FieldReadFunction<any>,
	settings?: FieldPolicy<any> | FieldReadFunction<any>,
	squad?: FieldPolicy<any> | FieldReadFunction<any>,
	squadById?: FieldPolicy<any> | FieldReadFunction<any>,
	squadInviteCode?: FieldPolicy<any> | FieldReadFunction<any>,
	unreadMessages?: FieldPolicy<any> | FieldReadFunction<any>,
	users?: FieldPolicy<any> | FieldReadFunction<any>
};
export type QuicollabDeviceKeySpecifier = ('createdOn' | 'globalName' | 'id' | 'isAuthorizedInSquad' | 'isOnline' | 'lastOnlineTime' | 'modifiedOn' | 'name' | 'type' | 'user' | 'userId' | 'ztDeviceIp' | 'ztNetDeviceId' | QuicollabDeviceKeySpecifier)[];
export type QuicollabDeviceFieldPolicy = {
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>,
	globalName?: FieldPolicy<any> | FieldReadFunction<any>,
	id?: FieldPolicy<any> | FieldReadFunction<any>,
	isAuthorizedInSquad?: FieldPolicy<any> | FieldReadFunction<any>,
	isOnline?: FieldPolicy<any> | FieldReadFunction<any>,
	lastOnlineTime?: FieldPolicy<any> | FieldReadFunction<any>,
	modifiedOn?: FieldPolicy<any> | FieldReadFunction<any>,
	name?: FieldPolicy<any> | FieldReadFunction<any>,
	type?: FieldPolicy<any> | FieldReadFunction<any>,
	user?: FieldPolicy<any> | FieldReadFunction<any>,
	userId?: FieldPolicy<any> | FieldReadFunction<any>,
	ztDeviceIp?: FieldPolicy<any> | FieldReadFunction<any>,
	ztNetDeviceId?: FieldPolicy<any> | FieldReadFunction<any>
};
export type QuicollabUserKeySpecifier = ('avatarFile' | 'avatarFileId' | 'claims' | 'createdOn' | 'devices' | 'email' | 'id' | 'isEnable' | 'loginProvider' | 'modifiedOn' | 'nickname' | 'openId' | 'orgCodes' | 'orgs' | 'password' | 'permissions' | 'phoneNumber' | 'roleIds' | 'roleNames' | 'roles' | 'tenantCode' | 'username' | QuicollabUserKeySpecifier)[];
export type QuicollabUserFieldPolicy = {
	avatarFile?: FieldPolicy<any> | FieldReadFunction<any>,
	avatarFileId?: FieldPolicy<any> | FieldReadFunction<any>,
	claims?: FieldPolicy<any> | FieldReadFunction<any>,
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>,
	devices?: FieldPolicy<any> | FieldReadFunction<any>,
	email?: FieldPolicy<any> | FieldReadFunction<any>,
	id?: FieldPolicy<any> | FieldReadFunction<any>,
	isEnable?: FieldPolicy<any> | FieldReadFunction<any>,
	loginProvider?: FieldPolicy<any> | FieldReadFunction<any>,
	modifiedOn?: FieldPolicy<any> | FieldReadFunction<any>,
	nickname?: FieldPolicy<any> | FieldReadFunction<any>,
	openId?: FieldPolicy<any> | FieldReadFunction<any>,
	orgCodes?: FieldPolicy<any> | FieldReadFunction<any>,
	orgs?: FieldPolicy<any> | FieldReadFunction<any>,
	password?: FieldPolicy<any> | FieldReadFunction<any>,
	permissions?: FieldPolicy<any> | FieldReadFunction<any>,
	phoneNumber?: FieldPolicy<any> | FieldReadFunction<any>,
	roleIds?: FieldPolicy<any> | FieldReadFunction<any>,
	roleNames?: FieldPolicy<any> | FieldReadFunction<any>,
	roles?: FieldPolicy<any> | FieldReadFunction<any>,
	tenantCode?: FieldPolicy<any> | FieldReadFunction<any>,
	username?: FieldPolicy<any> | FieldReadFunction<any>
};
export type RoleKeySpecifier = ('code' | 'createdOn' | 'id' | 'isDefault' | 'isEnabled' | 'isStatic' | 'modifiedOn' | 'name' | 'permissions' | 'tenantCode' | 'users' | RoleKeySpecifier)[];
export type RoleFieldPolicy = {
	code?: FieldPolicy<any> | FieldReadFunction<any>,
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>,
	id?: FieldPolicy<any> | FieldReadFunction<any>,
	isDefault?: FieldPolicy<any> | FieldReadFunction<any>,
	isEnabled?: FieldPolicy<any> | FieldReadFunction<any>,
	isStatic?: FieldPolicy<any> | FieldReadFunction<any>,
	modifiedOn?: FieldPolicy<any> | FieldReadFunction<any>,
	name?: FieldPolicy<any> | FieldReadFunction<any>,
	permissions?: FieldPolicy<any> | FieldReadFunction<any>,
	tenantCode?: FieldPolicy<any> | FieldReadFunction<any>,
	users?: FieldPolicy<any> | FieldReadFunction<any>
};
export type RolesCollectionSegmentKeySpecifier = ('items' | 'pageInfo' | 'totalCount' | RolesCollectionSegmentKeySpecifier)[];
export type RolesCollectionSegmentFieldPolicy = {
	items?: FieldPolicy<any> | FieldReadFunction<any>,
	pageInfo?: FieldPolicy<any> | FieldReadFunction<any>,
	totalCount?: FieldPolicy<any> | FieldReadFunction<any>
};
export type SettingKeySpecifier = ('createdOn' | 'id' | 'modifiedOn' | 'name' | 'scope' | 'scopedKey' | 'validScopes' | 'value' | SettingKeySpecifier)[];
export type SettingFieldPolicy = {
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>,
	id?: FieldPolicy<any> | FieldReadFunction<any>,
	modifiedOn?: FieldPolicy<any> | FieldReadFunction<any>,
	name?: FieldPolicy<any> | FieldReadFunction<any>,
	scope?: FieldPolicy<any> | FieldReadFunction<any>,
	scopedKey?: FieldPolicy<any> | FieldReadFunction<any>,
	validScopes?: FieldPolicy<any> | FieldReadFunction<any>,
	value?: FieldPolicy<any> | FieldReadFunction<any>
};
export type SettingsCollectionSegmentKeySpecifier = ('items' | 'pageInfo' | 'totalCount' | SettingsCollectionSegmentKeySpecifier)[];
export type SettingsCollectionSegmentFieldPolicy = {
	items?: FieldPolicy<any> | FieldReadFunction<any>,
	pageInfo?: FieldPolicy<any> | FieldReadFunction<any>,
	totalCount?: FieldPolicy<any> | FieldReadFunction<any>
};
export type SquadKeySpecifier = ('adminUserIds' | 'authorizedDevices' | 'autoAuthorizeDevices' | 'createdOn' | 'creatorId' | 'id' | 'inviteCode' | 'maxMemberCount' | 'modifiedOn' | 'name' | 'userIds' | 'users' | 'ztNetNetworkId' | SquadKeySpecifier)[];
export type SquadFieldPolicy = {
	adminUserIds?: FieldPolicy<any> | FieldReadFunction<any>,
	authorizedDevices?: FieldPolicy<any> | FieldReadFunction<any>,
	autoAuthorizeDevices?: FieldPolicy<any> | FieldReadFunction<any>,
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>,
	creatorId?: FieldPolicy<any> | FieldReadFunction<any>,
	id?: FieldPolicy<any> | FieldReadFunction<any>,
	inviteCode?: FieldPolicy<any> | FieldReadFunction<any>,
	maxMemberCount?: FieldPolicy<any> | FieldReadFunction<any>,
	modifiedOn?: FieldPolicy<any> | FieldReadFunction<any>,
	name?: FieldPolicy<any> | FieldReadFunction<any>,
	userIds?: FieldPolicy<any> | FieldReadFunction<any>,
	users?: FieldPolicy<any> | FieldReadFunction<any>,
	ztNetNetworkId?: FieldPolicy<any> | FieldReadFunction<any>
};
export type SquadCollectionSegmentKeySpecifier = ('items' | 'pageInfo' | 'totalCount' | SquadCollectionSegmentKeySpecifier)[];
export type SquadCollectionSegmentFieldPolicy = {
	items?: FieldPolicy<any> | FieldReadFunction<any>,
	pageInfo?: FieldPolicy<any> | FieldReadFunction<any>,
	totalCount?: FieldPolicy<any> | FieldReadFunction<any>
};
export type SquadDeviceStatusKeySpecifier = ('isAuthorized' | 'isUserSquadAdmin' | 'squadId' | 'squadName' | SquadDeviceStatusKeySpecifier)[];
export type SquadDeviceStatusFieldPolicy = {
	isAuthorized?: FieldPolicy<any> | FieldReadFunction<any>,
	isUserSquadAdmin?: FieldPolicy<any> | FieldReadFunction<any>,
	squadId?: FieldPolicy<any> | FieldReadFunction<any>,
	squadName?: FieldPolicy<any> | FieldReadFunction<any>
};
export type SubscriptionKeySpecifier = ('_' | 'echo' | 'onPrivateNotify' | 'onPublicNotify' | SubscriptionKeySpecifier)[];
export type SubscriptionFieldPolicy = {
	_?: FieldPolicy<any> | FieldReadFunction<any>,
	echo?: FieldPolicy<any> | FieldReadFunction<any>,
	onPrivateNotify?: FieldPolicy<any> | FieldReadFunction<any>,
	onPublicNotify?: FieldPolicy<any> | FieldReadFunction<any>
};
export type UserKeySpecifier = ('avatarFile' | 'avatarFileId' | 'claims' | 'createdOn' | 'email' | 'id' | 'isEnable' | 'loginProvider' | 'modifiedOn' | 'nickname' | 'openId' | 'orgCodes' | 'orgs' | 'permissions' | 'phoneNumber' | 'roleIds' | 'roleNames' | 'roles' | 'tenantCode' | 'username' | UserKeySpecifier)[];
export type UserFieldPolicy = {
	avatarFile?: FieldPolicy<any> | FieldReadFunction<any>,
	avatarFileId?: FieldPolicy<any> | FieldReadFunction<any>,
	claims?: FieldPolicy<any> | FieldReadFunction<any>,
	createdOn?: FieldPolicy<any> | FieldReadFunction<any>,
	email?: FieldPolicy<any> | FieldReadFunction<any>,
	id?: FieldPolicy<any> | FieldReadFunction<any>,
	isEnable?: FieldPolicy<any> | FieldReadFunction<any>,
	loginProvider?: FieldPolicy<any> | FieldReadFunction<any>,
	modifiedOn?: FieldPolicy<any> | FieldReadFunction<any>,
	nickname?: FieldPolicy<any> | FieldReadFunction<any>,
	openId?: FieldPolicy<any> | FieldReadFunction<any>,
	orgCodes?: FieldPolicy<any> | FieldReadFunction<any>,
	orgs?: FieldPolicy<any> | FieldReadFunction<any>,
	permissions?: FieldPolicy<any> | FieldReadFunction<any>,
	phoneNumber?: FieldPolicy<any> | FieldReadFunction<any>,
	roleIds?: FieldPolicy<any> | FieldReadFunction<any>,
	roleNames?: FieldPolicy<any> | FieldReadFunction<any>,
	roles?: FieldPolicy<any> | FieldReadFunction<any>,
	tenantCode?: FieldPolicy<any> | FieldReadFunction<any>,
	username?: FieldPolicy<any> | FieldReadFunction<any>
};
export type UserClaimKeySpecifier = ('claimType' | 'claimValue' | UserClaimKeySpecifier)[];
export type UserClaimFieldPolicy = {
	claimType?: FieldPolicy<any> | FieldReadFunction<any>,
	claimValue?: FieldPolicy<any> | FieldReadFunction<any>
};
export type UserTokenKeySpecifier = ('loginProvider' | 'name' | 'token' | 'user' | 'userId' | UserTokenKeySpecifier)[];
export type UserTokenFieldPolicy = {
	loginProvider?: FieldPolicy<any> | FieldReadFunction<any>,
	name?: FieldPolicy<any> | FieldReadFunction<any>,
	token?: FieldPolicy<any> | FieldReadFunction<any>,
	user?: FieldPolicy<any> | FieldReadFunction<any>,
	userId?: FieldPolicy<any> | FieldReadFunction<any>
};
export type UsersCollectionSegmentKeySpecifier = ('items' | 'pageInfo' | 'totalCount' | UsersCollectionSegmentKeySpecifier)[];
export type UsersCollectionSegmentFieldPolicy = {
	items?: FieldPolicy<any> | FieldReadFunction<any>,
	pageInfo?: FieldPolicy<any> | FieldReadFunction<any>,
	totalCount?: FieldPolicy<any> | FieldReadFunction<any>
};
export type StrictTypedTypePolicies = {
	AuditLog?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | AuditLogKeySpecifier | (() => undefined | AuditLogKeySpecifier),
		fields?: AuditLogFieldPolicy,
	},
	BlobObject?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | BlobObjectKeySpecifier | (() => undefined | BlobObjectKeySpecifier),
		fields?: BlobObjectFieldPolicy,
	},
	BlobObjectsCollectionSegment?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | BlobObjectsCollectionSegmentKeySpecifier | (() => undefined | BlobObjectsCollectionSegmentKeySpecifier),
		fields?: BlobObjectsCollectionSegmentFieldPolicy,
	},
	Captcha?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | CaptchaKeySpecifier | (() => undefined | CaptchaKeySpecifier),
		fields?: CaptchaFieldPolicy,
	},
	ClientNotify?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | ClientNotifyKeySpecifier | (() => undefined | ClientNotifyKeySpecifier),
		fields?: ClientNotifyFieldPolicy,
	},
	CollectionSegmentInfo?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | CollectionSegmentInfoKeySpecifier | (() => undefined | CollectionSegmentInfoKeySpecifier),
		fields?: CollectionSegmentInfoFieldPolicy,
	},
	DataChangeClientNotify?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | DataChangeClientNotifyKeySpecifier | (() => undefined | DataChangeClientNotifyKeySpecifier),
		fields?: DataChangeClientNotifyFieldPolicy,
	},
	DeviceAuthorizationStatusResult?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | DeviceAuthorizationStatusResultKeySpecifier | (() => undefined | DeviceAuthorizationStatusResultKeySpecifier),
		fields?: DeviceAuthorizationStatusResultFieldPolicy,
	},
	ExecutionHistoriesCollectionSegment?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | ExecutionHistoriesCollectionSegmentKeySpecifier | (() => undefined | ExecutionHistoriesCollectionSegmentKeySpecifier),
		fields?: ExecutionHistoriesCollectionSegmentFieldPolicy,
	},
	IAuthUser?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | IAuthUserKeySpecifier | (() => undefined | IAuthUserKeySpecifier),
		fields?: IAuthUserFieldPolicy,
	},
	IBlobObject?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | IBlobObjectKeySpecifier | (() => undefined | IBlobObjectKeySpecifier),
		fields?: IBlobObjectFieldPolicy,
	},
	IEntityBase?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | IEntityBaseKeySpecifier | (() => undefined | IEntityBaseKeySpecifier),
		fields?: IEntityBaseFieldPolicy,
	},
	IMessage?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | IMessageKeySpecifier | (() => undefined | IMessageKeySpecifier),
		fields?: IMessageFieldPolicy,
	},
	IMessageContent?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | IMessageContentKeySpecifier | (() => undefined | IMessageContentKeySpecifier),
		fields?: IMessageContentFieldPolicy,
	},
	IOrg?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | IOrgKeySpecifier | (() => undefined | IOrgKeySpecifier),
		fields?: IOrgFieldPolicy,
	},
	IPagedList?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | IPagedListKeySpecifier | (() => undefined | IPagedListKeySpecifier),
		fields?: IPagedListFieldPolicy,
	},
	IQuicollabUser?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | IQuicollabUserKeySpecifier | (() => undefined | IQuicollabUserKeySpecifier),
		fields?: IQuicollabUserFieldPolicy,
	},
	IRole?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | IRoleKeySpecifier | (() => undefined | IRoleKeySpecifier),
		fields?: IRoleFieldPolicy,
	},
	ISetting?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | ISettingKeySpecifier | (() => undefined | ISettingKeySpecifier),
		fields?: ISettingFieldPolicy,
	},
	IUser?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | IUserKeySpecifier | (() => undefined | IUserKeySpecifier),
		fields?: IUserFieldPolicy,
	},
	JobExecutionHistory?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | JobExecutionHistoryKeySpecifier | (() => undefined | JobExecutionHistoryKeySpecifier),
		fields?: JobExecutionHistoryFieldPolicy,
	},
	JobState?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | JobStateKeySpecifier | (() => undefined | JobStateKeySpecifier),
		fields?: JobStateFieldPolicy,
	},
	JobStateCollectionSegment?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | JobStateCollectionSegmentKeySpecifier | (() => undefined | JobStateCollectionSegmentKeySpecifier),
		fields?: JobStateCollectionSegmentFieldPolicy,
	},
	KeyValuePairOfStringAndObject?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | KeyValuePairOfStringAndObjectKeySpecifier | (() => undefined | KeyValuePairOfStringAndObjectKeySpecifier),
		fields?: KeyValuePairOfStringAndObjectFieldPolicy,
	},
	Message?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | MessageKeySpecifier | (() => undefined | MessageKeySpecifier),
		fields?: MessageFieldPolicy,
	},
	MessageDistribution?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | MessageDistributionKeySpecifier | (() => undefined | MessageDistributionKeySpecifier),
		fields?: MessageDistributionFieldPolicy,
	},
	MessagesCollectionSegment?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | MessagesCollectionSegmentKeySpecifier | (() => undefined | MessagesCollectionSegmentKeySpecifier),
		fields?: MessagesCollectionSegmentFieldPolicy,
	},
	Mutation?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | MutationKeySpecifier | (() => undefined | MutationKeySpecifier),
		fields?: MutationFieldPolicy,
	},
	NewMessageClientNotify?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | NewMessageClientNotifyKeySpecifier | (() => undefined | NewMessageClientNotifyKeySpecifier),
		fields?: NewMessageClientNotifyFieldPolicy,
	},
	Org?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | OrgKeySpecifier | (() => undefined | OrgKeySpecifier),
		fields?: OrgFieldPolicy,
	},
	OrgsCollectionSegment?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | OrgsCollectionSegmentKeySpecifier | (() => undefined | OrgsCollectionSegmentKeySpecifier),
		fields?: OrgsCollectionSegmentFieldPolicy,
	},
	Query?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | QueryKeySpecifier | (() => undefined | QueryKeySpecifier),
		fields?: QueryFieldPolicy,
	},
	QuicollabDevice?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | QuicollabDeviceKeySpecifier | (() => undefined | QuicollabDeviceKeySpecifier),
		fields?: QuicollabDeviceFieldPolicy,
	},
	QuicollabUser?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | QuicollabUserKeySpecifier | (() => undefined | QuicollabUserKeySpecifier),
		fields?: QuicollabUserFieldPolicy,
	},
	Role?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | RoleKeySpecifier | (() => undefined | RoleKeySpecifier),
		fields?: RoleFieldPolicy,
	},
	RolesCollectionSegment?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | RolesCollectionSegmentKeySpecifier | (() => undefined | RolesCollectionSegmentKeySpecifier),
		fields?: RolesCollectionSegmentFieldPolicy,
	},
	Setting?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | SettingKeySpecifier | (() => undefined | SettingKeySpecifier),
		fields?: SettingFieldPolicy,
	},
	SettingsCollectionSegment?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | SettingsCollectionSegmentKeySpecifier | (() => undefined | SettingsCollectionSegmentKeySpecifier),
		fields?: SettingsCollectionSegmentFieldPolicy,
	},
	Squad?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | SquadKeySpecifier | (() => undefined | SquadKeySpecifier),
		fields?: SquadFieldPolicy,
	},
	SquadCollectionSegment?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | SquadCollectionSegmentKeySpecifier | (() => undefined | SquadCollectionSegmentKeySpecifier),
		fields?: SquadCollectionSegmentFieldPolicy,
	},
	SquadDeviceStatus?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | SquadDeviceStatusKeySpecifier | (() => undefined | SquadDeviceStatusKeySpecifier),
		fields?: SquadDeviceStatusFieldPolicy,
	},
	Subscription?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | SubscriptionKeySpecifier | (() => undefined | SubscriptionKeySpecifier),
		fields?: SubscriptionFieldPolicy,
	},
	User?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | UserKeySpecifier | (() => undefined | UserKeySpecifier),
		fields?: UserFieldPolicy,
	},
	UserClaim?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | UserClaimKeySpecifier | (() => undefined | UserClaimKeySpecifier),
		fields?: UserClaimFieldPolicy,
	},
	UserToken?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | UserTokenKeySpecifier | (() => undefined | UserTokenKeySpecifier),
		fields?: UserTokenFieldPolicy,
	},
	UsersCollectionSegment?: Omit<TypePolicy, "fields" | "keyFields"> & {
		keyFields?: false | UsersCollectionSegmentKeySpecifier | (() => undefined | UsersCollectionSegmentKeySpecifier),
		fields?: UsersCollectionSegmentFieldPolicy,
	}
};
export type TypedTypePolicies = StrictTypedTypePolicies & TypePolicies;