import { MessageChannelKind } from 'src/app/core/services/messages-api.service';

export class ChannelState {
    constructor(public name: string, public description: string, public value: MessageChannelKind, public checked: boolean) { }
}
