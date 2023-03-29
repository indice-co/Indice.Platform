import { NotificationSubscription } from "../services/cases-api.service";

export class NotificationSubscriptionViewModel {
    public notificationSubscription: NotificationSubscription | undefined;
    public subscribed: boolean | undefined;
    public title: string | undefined;

    constructor(notificationSubscription: NotificationSubscription, subscribed: boolean, title: string) {
        this.notificationSubscription = notificationSubscription;
        this.subscribed = subscribed;
        this.title = title;
    }
}

export class NotificationSubscriptionCategoryViewModel {
    public name: string | undefined;
    public notificationSubscriptions: NotificationSubscriptionViewModel[] | undefined;
  }

export class DisplayNotificationSubscriptionsViewModel {
    public categories: NotificationSubscriptionCategoryViewModel[] | undefined;
  }