/** A case type the user can subscribe to for notifications. */
export interface NotificationSubscriptionViewModel {
    caseTypeId: string;
    title: string;
    subscribed: boolean;
}

/** Case types grouped under their case type category. */
export interface NotificationSubscriptionCategoryViewModel {
    name: string;
    notificationSubscriptions: NotificationSubscriptionViewModel[];
}
