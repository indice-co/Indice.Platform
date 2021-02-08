

WorkItems (Topic/Type, ..., Payload)

1. Topic (Quueue name) / Type => Producer
2. Topic (Quueue name) / Type => Consumer


WorkItemHandler => ProducerHandler --> enqueue
1. Wake every minute search database/source for items top batchsize items (1000)
2. Populate QUEUE with said size.
3. How can we mark those items as loaded? Potentialy by storing state somewhere regarding the last item loaded
    1. Intial Enqueue ProduceWork(last item id = null)
    2. Produce ConsumerWorkItems 

WorkItemHandler => CosumerHandler --> dequeue