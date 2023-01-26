// KeyFirePlugin.js
// Written and Authored by Kaveman (Org: KeyFire Studios)
// Cloned from: Kaveman/battleoftheforces.dev/src/scripts/KeyFirePlugin.js
var ConfigVersion = 'v1.2';
var RefreshRate = 100; // Do not edit (Manages Site Timer) <= Bruh seriously what was I trying to do?
var UpdateSpeed = 1;

function setNavPath() {
    var CurrentPathname = window.location.pathname;
    var CurrentPathwayArray = CurrentPathname.split('/');
    var ArrayLength = CurrentPathwayArray.length;
    var print = false;
    for (var i = 0; i < ArrayLength; i++) {
        var curr_path = CurrentPathwayArray[i];
        if (curr_path === '' && print === false) {
            print = true;
        }
        else if (curr_path === '') {
            path = path + '/';
        }
        else {
            curr_path = curr_path.charAt(0).toUpperCase() + curr_path.substring(1);
            path = path + curr_path;
        }
    }
    formatNavPath();
    document.getElementById('NavPath').innerHTML = document.getElementById('NavPath').innerHTML.replace('[USR_LOCATION]', path);
}

function GetPosition(type, Element) {
    var xPosition = 0;
    var yPosition = 0;

    while (Element) {
        xPosition += (Element.offsetLeft - Element.scrollLeft + Element.clientLeft);
        yPosition += (Element.offsetTop - Element.scrollTop + Element.clientTop);
        Element = Element.offsetParent;
    }

    if ((type === 'y') || (type === 'Y')) {
        return yPosition;
    }

    else if ((type === 'x') || (type === 'X')) {
        return xPosition;
    }
}

//This is the universal one that looks at both x and y axis to edit x and y axis
function Background3D(ElementID, DistanceX, OffsetX, DistanceY, OffsetY) {
    // Things for X-Axis
    var ViewPortTop = window.scrollY;
    var ViewPortMiddle = window.scrollY + (window.innerHeight / 2);
    var ViewPortBottom = window.scrollY + window.innerHeight;
    var DistanceOfViewPortTopToMiddle = ViewPortMiddle - ViewPortTop;
    var DistanceOfViewPortMiddleToBottom = ViewPortBottom - ViewPortMiddle;
    var PercentDistanceOfViewPortTopRoMiddle = DistanceOfViewPortTopToMiddle / 100;
    var PercentDistanceOfViewPortMiddleToBottom = DistanceOfViewPortMiddleToBottom / 100;
    // Values for X-Axis
    var ViewPortLeft = window.scrollX;
    var ViewPortCenter = window.scrollX + (window.innerWidth / 2);
    var ViewPortRight = window.scrollX + window.innerWidth;
    var DistanceOfViewPortLeftToCenter = ViewPortCenter - ViewPortLeft;
    var DistanceOfViewPortCenterToRight = ViewPortRight - ViewPortCenter;
    var PercentDistanceOfViewPortLeftToCenter = DistanceOfViewPortLeftToCenter / 100;
    var PercentDistanceOfViewPortCenterToRight = DistanceOfViewPortCenterToRight / 100;
    // First Text
    var TargetObject = document.getElementById(ElementID); // The element
    var StartingPointY = DistanceY; // Shadow Y Value (max value when out of view);
    var StartingPointX = DistanceX; // Shadow X Value (max value when out of view);
    //if (true) { // I thought that I would need this but probably not
        // Calculate for Y
        // Create range from zero
        var TopBorder = ViewPortTop - ViewPortTop; // Top is the top
        var MiddleBorder = ViewPortMiddle - ViewPortTop; // Middle is the bottom
        var ObjectPositionY = GetPosition('Y', TargetObject) - ViewPortTop; // Element position on the range
        var ObjectPositionPercentageInversed = ObjectPositionY / MiddleBorder; // This is the process that calculates but is reversed (wrong way), next line will put it right
        var ObjectPositionPercentage = (ObjectPositionPercentageInversed * (-1)); // Will know the exact value of a element
        var ObjectPropertyValueResult = (ObjectPositionPercentage * StartingPointY); //
        TargetObject.style.backgroundPositionY = ((ObjectPropertyValueResult * -1) + OffsetY).toString() + 'px'; // Set it
    //}
    //if (true) { // I thought that I would need this but probably not
        // Calculate for X
        // Create range from zero
        var LeftBorder = ViewPortLeft - ViewPortLeft; // Left is the Left
        var CenterBorder = ViewPortCenter - ViewPortLeft; // Center is the Bottom
        var ObjectPositionX = GetPosition('X', TargetObject) - ViewPortLeft; // Element position on the range
        var ObjectPositionXPercentageInversed = ObjectPositionX / CenterBorder; // This is the process that calculates but is reversed (wrong way), next line will put it right
        var ObjectPositionXPercentage = (ObjectPositionXPercentageInversed * (-1)); // Will know the exact value of a element
        var ObjectPropertyXValueResult = (ObjectPositionXPercentage * StartingPointX); //
        TargetObject.style.backgroundPositionX = ((ObjectPropertyXValueResult * -1) + OffsetX).toString() + 'px'; // Set it
    //}
}

// This one
function Object3DTransform(ElementID, DistanceX, OffsetX, DistanceY, formatStringY) {
    // Things for X-Axis
    var ViewPortTop = window.scrollY;
    var ViewPortMiddle = window.scrollY + (window.innerHeight / 2);
    var ViewPortBottom = window.scrollY + window.innerHeight;
    var DistanceOfViewPortTopToMiddle = ViewPortMiddle - ViewPortTop;
    var DistanceOfViewPortMiddleToBottom = ViewPortBottom - ViewPortMiddle;
    var PercentDistanceOfViewPortTopRoMiddle = DistanceOfViewPortTopToMiddle / 100;
    var PercentDistanceOfViewPortMiddleToBottom = DistanceOfViewPortMiddleToBottom / 100;
    // Values for X-Axis
    var ViewPortLeft = window.scrollX;
    var ViewPortCenter = window.scrollX + (window.innerWidth / 2);
    var ViewPortRight = window.scrollX + window.innerWidth;
    var DistanceOfViewPortLeftToCenter = ViewPortCenter - ViewPortLeft;
    var DistanceOfViewPortCenterToRight = ViewPortRight - ViewPortCenter;
    var PercentDistanceOfViewPortLeftToCenter = DistanceOfViewPortLeftToCenter / 100;
    var PercentDistanceOfViewPortCenterToRight = DistanceOfViewPortCenterToRight / 100;
    // First Text
    var TargetObject = document.getElementById(ElementID); // The element
    var StartingPointY = DistanceY; // Shadow Y Value (max value when out of view);
    var StartingPointX = DistanceX; // Shadow X Value (max value when out of view);
    //if (true) { // I thought that I would need this but probrably not
        // Calculate for Y
        // Create range from zero
        var TopBorder = ViewPortTop - ViewPortTop; // Top is the top
        var MiddleBorder = ViewPortMiddle - ViewPortTop; // Middle is the bottom
        var ObjectPositionY = GetPosition('Y', TargetObject) - ViewPortTop; // Element postion on the range
        var ObjectPositionPercentageInversed = ObjectPositionY / MiddleBorder; // This is the procees that calculates but is reversed (wrong way), next line will put it right
        var ObjectPositionPercentage = (ObjectPositionPercentageInversed * (-1)); // Will know the exact value of a element
        var ObjectPropertyValueResult = (ObjectPositionPercentage * StartingPointY); //
        TargetObject.style.backgroundPositionY = formatStringY.replace("{0}", ObjectPropertyValueResult.toString() + 'px'); // Set it
    //}
    //if (true) { // I thought that I would need this but probably not
        // Calculate for X
        // Create range from zero
        var LeftBorder = ViewPortLeft - ViewPortLeft; // Left is the Left
        var CenterBorder = ViewPortCenter - ViewPortLeft; // Center is the Bottom
        var ObjectPositionX = GetPosition('X', TargetObject) - ViewPortLeft; // Element postion on the range
        var ObjectPositionXPercentageInversed = ObjectPositionX / CenterBorder; // This is the procees that calculates but is reversed (wrong way), next line will put it right
        var ObjectPositionXPercentage = (ObjectPositionXPercentageInversed * (-1)); // Will know the exact value of a element
        var ObjectPropertyXValueResult = (ObjectPositionXPercentage * StartingPointX); //
        //TargetObject.style.transform = "translateY(" + OffsetY.toString() + "px)"; // Set it
    //}
}