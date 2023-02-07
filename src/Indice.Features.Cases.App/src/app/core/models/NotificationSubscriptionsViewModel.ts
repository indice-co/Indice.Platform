import { NotificationSubscription } from "../services/cases-api.service";

export class NotificationSubscriptionViewModel {
    public notificationSubscription: NotificationSubscription | undefined;
    public subscribed: boolean | undefined;
    public title: string | undefined;

    constructor(notificationSubscription: NotificationSubscription, subscribed: boolean) {
        this.notificationSubscription = notificationSubscription;
        this.subscribed = subscribed;
    }
}