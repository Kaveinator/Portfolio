const InitialCalculatorState = {
  DisplayValue: "0",
  FirstOperand: null, // First part of equation
  WaitSecondOperand: false, // Is waiting for initial input for second part of equation
  Operator: null // +, -, *, /, etc
},
Clone = ref => JSON.parse(JSON.stringify(ref)), // From tictactoe line 113, JS normally passes thing by refrence, this clones it
IsNullOrUndefined = obj => !(obj && obj !== 'null' && obj !== 'undefined'), // Method name from C#
IsNullOrEmpty = str => IsNullOrUndefined(str) || str.length == 0; // Simillar method from string.IsNullOrEmpty() in C#
DisplayElem = document.querySelector(".calculatorScreen");
var Calculator = Clone(InitialCalculatorState);

// Appends a digit to current input
function InputDigit(digit) {
  const { DisplayValue, WaitSecondOperand } = Calculator;

  if (WaitSecondOperand) {
    Calculator.DisplayValue = digit;
    Calculator.WaitSecondOperand = false;  
  }
  else Calculator.DisplayValue = (DisplayValue == "0" ? "" : DisplayValue) + digit;
}

// Adds a decimal point, if possible
function InputDecimal(dot) {
  if (Calculator.WaitSecondOperand) return; // Return if no initial value was set (or maybe InputDigit(0) then continue?)
  if (!Calculator.DisplayValue.includes(dot)) // Check if a decimal point already exists
    Calculator.DisplayValue += dot; // If not append it
}

function HandleOperator(NextOperator) {
  const { FirstOperand, WaitSecondOperand, DisplayValue, Operator} = Calculator,
    ValueOfInput = parseFloat(DisplayValue);
  if (Operator && WaitSecondOperand) {
    Calculator.Operator = NextOperator;
    return;  
  }
  if (IsNullOrUndefined(FirstOperand))
    Calculator.FirstOperand = ValueOfInput;
  else if (!IsNullOrUndefined(Operator)) {
    const CurrentValue = FirstOperand || 0;
    let result = PerformCalculation[Operator](CurrentValue, ValueOfInput);
    result = (result * 1).toString();
    Calculator.DisplayValue = Calculator.FirstOperand = parseFloat(result);
  }
  Calculator.WaitSecondOperand = true;
  Calculator.Operator= NextOperator;
}

const PerformCalculation = {
  '/': (first, second) => first / second,
  '*': (first, second) => first * second,
  '+': (first, second) => first + second,
  '-': (first, second) => first - second,
  '=': (_, value) => value,
};

function Reset() {
  Calculator = Clone(InitialCalculatorState);
  UpdateDisplay();
}

function UpdateDisplay() {
  DisplayElem.value = IsNullOrEmpty(Calculator.DisplayValue) ? "0" : Calculator.DisplayValue;
}

UpdateDisplay();
document.addEventListener('click', e => {
  const { target } = e;
  if (!target.matches("button")) return;
  if (target.classList.contains('operator'))
    HandleOperator(target.value);
  else if (target.classList.contains("decimal"))
    InputDecimal(target.value);
  else if (target.classList.contains("clearAll"))
    Reset();
  else InputDigit(target.value);
  UpdateDisplay();
});