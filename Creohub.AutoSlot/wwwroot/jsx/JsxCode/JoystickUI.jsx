
// Create the main window
var win = new Window("palette", "Joystick Control Panel", undefined, {resizeable: false});

// Create buttons for UP, DOWN, LEFT, RIGHT, WIN, and CLEAR
win.upButton = win.add("button", undefined, "UP");
win.downButton = win.add("button", undefined, "DOWN");
win.leftButton = win.add("button", undefined, "LEFT");
win.rightButton = win.add("button", undefined, "RIGHT");
win.winButton = win.add("button", undefined, "WIN");
win.clearButton = win.add("button", undefined, "CLEAR");

// Define actions for each button
win.upButton.onClick = function() {
    JOYSTICK.Up();
};

win.downButton.onClick = function() {
    JOYSTICK.Down(1);
};

win.leftButton.onClick = function() {
    JOYSTICK.Left();
};

win.rightButton.onClick = function() {
    JOYSTICK.Right();
};

win.winButton.onClick = function() {
    JOYSTICK.Win();
};

win.clearButton.onClick = function() {
    JOYSTICK.Clear();
};

// Layout the buttons in a grid-like pattern
win.upButton.alignment = ['center', 'top'];
win.downButton.alignment = ['center', 'bottom'];
win.leftButton.alignment = ['left', 'center'];
win.rightButton.alignment = ['right', 'center'];
win.winButton.alignment = ['left', 'top'];
win.clearButton.alignment = ['right', 'top'];

// Show the window
win.center();
win.show();