const CanvasElem = document.getElementById("WinLines"),
  CanvasContext = CanvasElem.getContext("2d"),
  ResultTextElem = document.getElementById("ResultText"),
  AllSquares = document.getElementsByClassName("square"),
  GetAvailableSquares = () => document.querySelectorAll(".square:not(.x):not(.o)"),
  WinningConditions = [ // Contains all square combinations to win
    [0, 1, 2],
    [3, 4, 5],
    [6, 7, 8],
    [0, 4, 8],
    [1, 4, 7],
    [2, 4, 6],
    [5, 4, 3],
    [0, 3, 6],
    [2, 5, 8]
  ],
  Players = ['x', 'o'],
  PlayerNames = [ "Player" , "Computer"],
  LocalPlayer = Players[0],
  // This is used for square positions, used when drawing lines
  MIN = 100, MID = 304, MAX = 508;
  SquareCords = {
    "0" : [MIN, MIN],
    "3" : [MIN, MID],
    "6" : [MIN, MAX],
  
    "2" : [MAX, MIN],
    "5" : [MAX, MID],
    "8" : [MAX, MAX],
  
    "1": [MID, MIN],
    "7": [MID, MAX]
  },
  DEBUG = false; // Was used to show box cords and some logging (most was removed)
let ActivePlayer = LocalPlayer,
  PlayedSquares = [],
  Ongoing = false;

// A function do make a play
function Play(currentElem, asComputer = false) {
  if (ActivePlayer != LocalPlayer && !asComputer || !Ongoing) return false;
  let squareIndex = Number(currentElem.id)
  if (!PlayedSquares.some(e => e == currentElem)) {
    currentElem.classList.add(ActivePlayer);
    PlayedSquares.push(`${squareIndex}${ActivePlayer}`);
    CheckWinConditions();
    ActivePlayer = ActivePlayer == 'x' ? 'o' : 'x';
    PlayOneShot("./media/place.mp3");
    if (ActivePlayer == 'o')
      setTimeout(ComputersTurn, 500);
  }
  return true;
}

function ComputersTurn() {
  let cachedSquares = GetAvailableSquares();
  if (cachedSquares.length == 0) return;
  let chosenSquareElem = cachedSquares[Math.floor(Math.random() * cachedSquares.length)];
  Play(chosenSquareElem, true);
}

function CheckWinConditions() {
  let winnerIndex = undefined,
    winningCombo = [];
  // Goes through and checks if either player has the combo
  function _checkWin(condition) {
    for (let i = 0; i < Players.length; i++) {
      let team = Players[i];
      if (condition.every(squareNum => PlayedSquares.includes(`${squareNum}${team}`))) {
        winnerIndex = i;
        winningCombo = condition;
        return true;
      }
    }
    return false;
  }
  // Check for any winning conditions
  if (!WinningConditions.some(_checkWin)) {
    // If not, check if its a draw
    if (GetAvailableSquares().length == 0) {
      Ongoing = false;
      PlayOneShot("./media/draw.ogg");
      console.log("tie");
      ResultTextElem.innerText = `Draw, Play Again?`;
      ResultTextElem.classList.add("enabled");
    }
    return;
  }
  let winnerName = PlayerNames[winnerIndex];
  console.log(`${winnerName} won!`);
  PlayOneShot(winnerIndex == 0 ? "./media/win.ogg" : "./media/lose.mp3");
  DrawLine(winningCombo);
  ResultTextElem.innerText = `${winnerName} won! Play Again?`;
  ResultTextElem.classList.add("enabled");
  Ongoing = false;
}

// Plays an audio clip, "PlayOneShot" is named after Unity's "PlayOneShot" methodd
let AudioSources = [];
function PlayOneShot(audioClipUrl = "") {
  definedAudioSource = AudioSources.find(tuple => tuple[0] == audioClipUrl);
  if (definedAudioSource == undefined) {
    definedAudioSource = [ audioClipUrl, new Audio(audioClipUrl) ];
    AudioSources.push(definedAudioSource);
  }
  definedAudioSource[1].play();
}

// Draw a line on the canvas
function DrawLine(winningCombo) {
  let startPos = SquareCords[winningCombo[0]],
    targetPos = SquareCords[winningCombo[2]];
    activePos = JSON.parse(JSON.stringify(startPos)), // Why string/parse? Because doing = startPos refrences it rather than copies it
    stop = [false, false]; // Stop for each axis: stopX, stopY
  function _update() {
    if (stop.every(i => i)) return;
    // Go through each dimension (x and y in this case)
    for (let i = 0 ; i < activePos.length; i++) {
      if (stop[i]) continue;
      let active = activePos[i],
        target = targetPos[i],
        // This func tells us if it should go up/down (1/-1) or none (0)
        evalMultiplier = () => active == target ? 0
          : active > target ? -1 : 1,
        multiplier = evalMultiplier();
      // Apply change
      active += 10 * multiplier;
      // If we overshot (eg: we need to go down instead of up (or vice versa))
      // or if we don't need to change anything, return
      if (evalMultiplier() != multiplier || multiplier == 0) {
        stop[i] = true;
        active = target;
      }
      if (DEBUG && i == 0) console.log(`[${startPos}] [${activePos}] [${targetPos}] [${stop}]`);
      activePos[i] = active; 
    }
    _ = requestAnimationFrame(_update);
    CanvasContext.clearRect(0, 0, 608, 608);
    CanvasContext.beginPath();
    CanvasContext.moveTo(startPos[0], startPos[1]);
    CanvasContext.lineTo(activePos[0], activePos[1]);
    CanvasContext.lineWidth = 10;
    CanvasContext.strokeStyle = "rgba(70, 255, 33, 0.8)";
    CanvasContext.stroke();
  }
  _update();
}

function Reset() {
  // Go through all squares and remove its background
  for (let squareIndex = 0; squareIndex < AllSquares.length; squareIndex++) {
    let elem = AllSquares[squareIndex];
    for (let teamIndex = 0; teamIndex < Players.length; teamIndex++)
      elem.classList.remove(Players[teamIndex]);
  }
  // Clear canvas, and reset active player
  CanvasContext.clearRect(0, 0, 608, 608);
  ActivePlayer = LocalPlayer;
  ResultTextElem.classList.remove("enabled");
  PlayedSquares = [];
  Ongoing = true;
  // Debug square cords
  if (DEBUG) {
    let keys = Object.keys(SquareCords);
    for (let i = 0; i < keys.length; i++) {
      let cord = SquareCords[keys[i]];
      CanvasContext.fillStyle = "#f00";
      CanvasContext.fillRect(cord[0], cord[1], 5, 5);
    }
  }
}