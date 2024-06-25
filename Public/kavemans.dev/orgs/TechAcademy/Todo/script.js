const TaskState = { Queued: 0, Ongoing: 1, Done: 2 },
  Feeds = {
    0: document.querySelector(".feed.queued"),
    1: document.querySelector(".feed.ongoing"),
    2: document.querySelector(".feed.done")
  },
  PrefabsElem = document.getElementById("Prefabs"),
  TaskItemTemplateElem = PrefabsElem.querySelector(".item"),
  MoveElem = (targetElem, parent) => parent.appendChild(targetElem.parentElement.removeChild(targetElem)),
  CreateTask = taskName => {
    if (taskName.length == 0) return;
    let instantiatedElem = TaskItemTemplateElem.cloneNode(true),
      actionsContainer = instantiatedElem.querySelector(".actionsContainer"),
      task = {
        State: TaskState.Queued,
        SecondsElapsed: 0,
        Elem: instantiatedElem,
        DoneToggleButton: instantiatedElem.querySelector(".checkbox"),
        TextElem: instantiatedElem.querySelector(".text"),
        TimerElem: instantiatedElem.querySelector(".timer"),
        ActionsContainer: actionsContainer,
        Actions: {
          StartButton: actionsContainer.querySelector(".start"),
          StopButton: actionsContainer.querySelector(".stop"),
          EditButton: actionsContainer.querySelector(".edit"),
          DeleteButton: actionsContainer.querySelector(".delete")
        },
        UpdateTimer: () => {
          let minutes = Math.floor(task.SecondsElapsed / 60),
            seconds = task.SecondsElapsed % 60,
            divider = seconds < 10 ? ':0' : ':';
          task.TimerElem.innerText = `${minutes}${divider}${seconds}`;
        }
    };
    // Add actions
    task.Elem.addEventListener("dblclick", () => task.TextElem.innerText = prompt(`Rename '${task.TextElem.innerText}':`) ?? task.TextElem.innerText);
    task.DoneToggleButton.addEventListener("click", () => MoveElem(task.Elem, Feeds[task.State = task.State == TaskState.Done ? TaskState.Queued : TaskState.Done]));
    task.Actions.StartButton.addEventListener("click", () => MoveElem(task.Elem, Feeds[task.State = TaskState.Ongoing]));
    task.Actions.StopButton.addEventListener("click", () => MoveElem(task.Elem, Feeds[task.State = TaskState.Queued]));
    task.Actions.EditButton.addEventListener("click", () => task.TextElem.innerText = prompt(`Rename '${task.TextElem.innerText}':`) ?? task.TextElem.innerText);
    task.Actions.DeleteButton.addEventListener("click", () => {
      if (!confirm(`Delete '${task.TextElem.innerText}'?`))
        return;
      var taskIndex = Tasks.indexOf(task);
      if (taskIndex == -1) return;
      Tasks.splice(taskIndex, 1);
      task.Elem.remove();
    });
    task.TextElem.innerText = taskName;
    Feeds[task.State].appendChild(task.Elem);
    Tasks.push(task);
    return task;
  }
const Tasks = [];

function Start() {
  let lastTasks = JSON.parse(localStorage.getItem("Tasks"));
  if (lastTasks != null) {
    lastTasks.forEach(taskInfo => {
      let task = CreateTask(taskInfo.Content);
      if (task.State != taskInfo.State)
        MoveElem(task.Elem, Feeds[task.State = taskInfo.State]);
      task.TextElem.innerText = taskInfo.Content;
      if ((task.SecondsElapsed = taskInfo.SecondsElapsed) > 0)
        task.UpdateTimer();
    });
  }
  setInterval(Update, 1000);
}

function OnDestroy() {
  let persistentTasks = [];
  Tasks.forEach(task => {
    persistentTasks.push({
      State: task.State,
      SecondsElapsed: task.SecondsElapsed,
      Content: task.TextElem.innerText
    });
  });
  localStorage.setItem("Tasks", JSON.stringify(persistentTasks));
}

function Update() {
  Tasks.filter(task => task.State == TaskState.Ongoing).forEach(task => {
    task.SecondsElapsed++;
    task.UpdateTimer();
  });
}