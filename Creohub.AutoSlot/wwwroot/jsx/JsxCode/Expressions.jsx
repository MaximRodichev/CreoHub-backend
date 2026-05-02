// /*
//     Хранилище всех экспрешенов (устаревшее)
// */
// var EXPRESSIONS = {
//     mainLine_yPosition: 'try{comp(AutoSlotComp).layer(thisComp.name).effect("yPosition")(1).valueAtTime(comp(AutoSlotComp).layer(thisComp.name).startTime + time)}catch(err){thisProperty}',
//     mainLine_scale: 'try{comp(AutoSlotComp).layer(thisComp.name).effect("Scale")(1).valueAtTime(comp(AutoSlotComp).layer(thisComp.name).startTime + time)}catch(err){thisProperty}',
//     mainLine_rotation: 'try{comp(AutoSlotComp).layer(thisComp.name).effect("Rotation")(1).valueAtTime(comp(AutoSlotComp).layer(thisComp.name).startTime + time)}catch(err){thisProperty}',

//     nullOfLine_xPosition: 'self = transform.xPosition;\
//         indexThis = parseInt(thisLayer.name.split("_")[1])\
//         spacing = comp(SlotCompName).layer("CONFIGURATION DONOTDELETE").effect("SpacingBetweenElements_Horizontal")(1);\
//         countLines = comp(SlotCompName).layer("CONFIGURATION DONOTDELETE").effect("ElementsByWidth")(1) + 1;\
//         (thisComp.width - spacing ) / countLines * (indexThis+1) + spacing/2;',
//     nullOfLine_yPosition: 'self = transform.yPosition\
//         indexThis = parseInt(thisLayer.name.split("_")[1])\
//         param =  comp(SlotCompName).layer("CONFIGURATION DONOTDELETE").effect("SlowLines")(1)\
//         null_ = thisComp.layer("MainLine").transform.yPosition.valueAtTime(time-param*indexThis)\
//         spacing = comp(SlotCompName).layer("CONFIGURATION DONOTDELETE").effect("SpacingBetweenElements_Vertical")(1);\
//         countLines = comp(SlotCompName).layer("CONFIGURATION DONOTDELETE").effect("ElementsByHeight")(1);\
//         start_ = (thisComp.height + spacing) / (countLines+1) + spacing;\
//         self + null_ + spacing * (countLines/2+1)',

//     nullOfLine_scale: 'self = transform.scale\
//         indexThis = parseInt(thisLayer.name.split("_")[1])\
//         param =  comp(SlotCompName).layer("CONFIGURATION DONOTDELETE").effect("SlowLines")(1)\
//         null_ = thisComp.layer("MainLine").transform.scale.valueAtTime(time-param*indexThis)\
//         result = [self[0] * (null_[0]/100),self[1] * (null_[1]/100)];\
//         result',
//     nullOfLine_rotation: 'self = transform.rotation\
//         indexThis = parseInt(thisLayer.name.split("_")[1])\
//         param =  comp(SlotCompName).layer("CONFIGURATION DONOTDELETE").effect("SlowLines")(1)\
//         null_ = thisComp.layer("MainLine").transform.rotation.valueAtTime(time-param*indexThis)\
//         result = self + null_;\
//         result;',
    
//     element_xPosition: 'thisComp.layer(nullNaming).transform.xPosition + transform.xPosition',
//     element_yPosition: 'spacing = comp(SlotCompName).layer("CONFIGURATION DONOTDELETE").effect("SpacingBetweenElements_Vertical")(1);\
//         param = comp(SlotCompName).layer("CONFIGURATION DONOTDELETE").effect("SlowMain")(1)\
//         self = transform.yPosition;\
//         null_ = thisComp.layer(target).transform.yPosition.valueAtTime(time-param);\
//         countLines = comp(SlotCompName).layer("CONFIGURATION DONOTDELETE").effect("ElementsByHeight")(1);\
//         start_ = (thisComp.height + spacing) / (countLines+1) + spacing;\
//         null_ - start_ + self ',
//     element_yPositionNew: 'null_ = thisComp.layer(target).transform.yPosition.valueAtTime(time-param);\
//         null_ + transform.yPosition ',
//     element_scale: 'upScaleSpin = thisComp.layer("MainLine").effect("ScaleControl")(1)\
//                     upScaleAnimation = thisComp.layer(target).transform.scale.valueAtTime(time-param);\
//                     result = transform.scale * (upScaleSpin/100);\
//                     [result[0]*(upScaleAnimation[0]/100), result[1]*(upScaleAnimation[1]/100)]',
    
//     element_rotation:  'null_ = thisComp.layer(target).transform.rotation.valueAtTime(time-param);\
//                         null_  + transform.rotation ',
//     element_winAnimation: 'DisposalTime = comp(SlotCompName).layer("CONFIGURATION DONOTDELETE").effect("SlowWin")(1) * DisposalIndex\
//                         if(thisComp.marker.key("StartAnimation").time + DisposalTime > time || time < thisComp.marker.key("EndAnimation").time + DisposalTime)\
//                         {time - thisComp.marker.key("StartAnimation").time - DisposalTime}\
//                         else {0}'
// };
/**
    Хранилище всех экспрешенов
*/
var EXPRESSIONS_V2 = (function() {
    return {
        mainLine_yPosition: function(AutoSlotComp){
            return 'try{comp("'+(AutoSlotComp)+'").layer(thisComp.name).effect("yPosition")(1).valueAtTime(comp("'+(AutoSlotComp)+'").layer(thisComp.name).startTime + time)}catch(err){thisProperty}'
        },
        mainLine_scale: function(AutoSlotComp){
            return 'try{comp("'+(AutoSlotComp)+'").layer(thisComp.name).effect("Scale")(1).valueAtTime(comp("'+(AutoSlotComp)+'").layer(thisComp.name).startTime + time)}catch(err){thisProperty}'
        },
        mainLine_rotation: function(AutoSlotComp){
            return 'try{comp("'+(AutoSlotComp)+'").layer(thisComp.name).effect("Rotation")(1).valueAtTime(comp("'+(AutoSlotComp)+'").layer(thisComp.name).startTime + time)}catch(err){thisProperty}'
        },
        nullOfLine_yPosition: function(SlotCompName){
            return 'self = transform.yPosition\
            indexThis = parseInt(thisLayer.name.split("_")[1])\
            param =  comp("'+(SlotCompName)+'").layer("CONFIGURATION DONOTDELETE").effect("SlowLines")(1)\
            null_ = thisComp.layer("MainLine").transform.yPosition.valueAtTime(time-param*indexThis)\
            spacing = comp("'+(SlotCompName)+'").layer("CONFIGURATION DONOTDELETE").effect("SpacingBetweenElements_Vertical")(1);\
            countLines = comp("'+(SlotCompName)+'").layer("CONFIGURATION DONOTDELETE").effect("ElementsByHeight")(1);\
            start_ = (thisComp.height + spacing) / (countLines+1) + spacing;\
            self + null_ + spacing * (countLines/2+1)'
        },
        nullOfLine_scale: function(SlotCompName){
            return 'self = transform.scale\
            indexThis = parseInt(thisLayer.name.split("_")[1])\
            param =  comp("'+(SlotCompName)+'").layer("CONFIGURATION DONOTDELETE").effect("SlowLines")(1)\
            null_ = thisComp.layer("MainLine").transform.scale.valueAtTime(time-param*indexThis)\
            result = [self[0] * (null_[0]/100),self[1] * (null_[1]/100)];\
            result'
        },
        nullOfLine_rotation: function(SlotCompName) {
            return 'self = transform.rotation\
            indexThis = parseInt(thisLayer.name.split("_")[1])\
            param =  comp("'+(SlotCompName)+'").layer("CONFIGURATION DONOTDELETE").effect("SlowLines")(1)\
            null_ = thisComp.layer("MainLine").transform.rotation.valueAtTime(time-param*indexThis)\
            result = self + null_;\
            result;'
        },
        nullOfLine_yPosition_NoControlled: function(SlowLines){
            return 'thisComp.layer("MainLine").transform.yPosition.valueAtTime(time-'+(SlowLines)+') + transform.yPosition'
        },
        nullOfLine_scale_NoControlled: function(SlowLines){
            return 'self = transform.scale\
            null_ = thisComp.layer("MainLine").transform.scale.valueAtTime(time-'+SlowLines+')\
            result = [self[0] * (null_[0]/100),self[1] * (null_[1]/100)];\
            result'
        },
        nullOfLine_rotation_NoControlled: function(SlowLines) {return 'self = transform.rotation\
            null_ =  comp("Slot").layer(thisComp.name).effect("Rotation")(1)\
			expand = comp("Slot").layer(thisComp.name).startTime\
            result = self + null_.valueAtTime(time-'+SlowLines+' + expand);\
            result;'
        },
        element_xPosition: function(nullNaming) {
            return 'thisComp.layer("'+(nullNaming)+'").transform.xPosition + transform.xPosition';
        },
        element_yPosition: function(SlotCompName, target) {
            return 'spacing = comp("'+(SlotCompName)+'").layer("CONFIGURATION DONOTDELETE").effect("SpacingBetweenElements_Vertical")(1);\
                param = comp("'+(SlotCompName)+'").layer("CONFIGURATION DONOTDELETE").effect("SlowMain")(1)\
                self = transform.yPosition;\
                null_ = thisComp.layer("'+(target)+'").transform.yPosition.valueAtTime(time-param);\
                countLines = comp("'+(SlotCompName)+'"e).layer("CONFIGURATION DONOTDELETE").effect("ElementsByHeight")(1);\
                start_ = (thisComp.height + spacing) / (countLines+1) + spacing;\
                null_ - start_ + self '
        },
        element_yPositionNew: function(target, param) {
            return 'thisComp.layer("'+(target)+'").transform.yPosition.valueAtTime(time-'+(param)+') + transform.yPosition;'
        },
        element_scale: function(target, param){
            return 'upScaleSpin = thisComp.layer("MainLine").effect("ScaleControl")(1)\
            upScaleAnimation = thisComp.layer("'+(target)+'").transform.scale.valueAtTime(time-'+(param)+');\
            result = transform.scale * (upScaleSpin/100);\
            [result[0]*(upScaleAnimation[0]/100), result[1]*(upScaleAnimation[1]/100)]'
        },
        element_rotation: function(target, param){ 
            return 'thisComp.layer("'+(target)+'").transform.rotation.valueAtTime(time-'+(param)+') + transform.rotation'
        },
        element_winAnimation: function(SlowWin, DisposalIndex){
            return 'DisposalTime = '+(SlowWin)+' * '+(DisposalIndex)+'\
            if(thisComp.marker.key("StartAnimation").time + DisposalTime > time || time < thisComp.marker.key("EndAnimation").time + DisposalTime)\
            {time - thisComp.marker.key("StartAnimation").time - DisposalTime}\
            else {0}'
        },
        element_Y_joystick: function(nameTarget, param, paramExtended, elementsByHeight, destination){
            destination = destination.toFixed(2)
            return 'target_ = thisComp.layer("'+ (nameTarget) +'").transform.yPosition\
            var swapParam = transform.yPosition;\
            if(marker.numKeys > 0){\
                var coef = 0\
                var coefX = '+(nameTarget.split("_")[1])+'\
                var last_ = transform.yPosition\
                for(x = 1; x <= marker.numKeys; x++){\
                    if(marker.key(x).time <= time) {\
                        a = last_\
                        action = marker.key(x).comment.split("_")\
                        if(marker.key(x).comment == "Up"){\
                            coef = coef - 1\
                            b = a + '+(destination)+'\
                            animParam = marker.key(x).duration\
                        }\
                        else if(marker.key(x).comment == "left"){\
                            coefX = coefX - 1\
                            target_ = thisComp.layer("Line_"+String(coefX)).transform.yPosition\
                            continue;\
                        }\
                        else if(marker.key(x).comment == "right"){\
                            coefX = coefX + 1\
                            target_ = thisComp.layer("Line_"+String(coefX)).transform.yPosition\
                            continue;\
                        }\
                        else if(action[0] == "Fall"){\
                            coef = coef + parseInt(action[1])\
                            b = a - '+(destination)+' * parseInt(action[1])\
                            animParam = marker.key(x).duration\
                        }\
                        else{\
							b = a\
							animParam = 0\
						}\
                        last_ = b\
                        swapParam = ease(time, marker.key(x).time, marker.key(x).time + animParam , a, b)\
                    }\
                }\
                target_.valueAtTime(time-'+(param)+' + ('+(paramExtended)+' * coef)) + swapParam\
            }else{target_.valueAtTime(time-'+(param)+') + swapParam}'
        },
        element_X_joystick: function(spacingValue){
            return 'if(marker.numKeys>0){\
                var swapParam =  transform.xPosition\
                var last_ = transform.xPosition\
                for(x=1; x <= marker.numKeys; x++){\
                    if(marker.key(x).time <= time){\
                        a = last_\
                        if(marker.key(x).comment == "left"){\
                            b = a - '+(spacingValue)+';\
                            animParam = marker.key(x).duration\
                        }\
                        else if(marker.key(x).comment == "right"){\
                            b = a + '+(spacingValue)+';\
                            animParam = marker.key(x).duration\
                        }\
                        else{\
                            b=a;\
                            animParam=0;\
                        }\
                        last_ = b;\
                        swapParam = ease(time, marker.key(x).time, marker.key(x).time + animParam , a, b)\
                    }\
                }\
            swapParam\
            }\
            else{\
                transform.xPosition\
            }'
        },
        element_Win_Joystick: function(){
            return 'result=0\
                    for(x = 1; x <= marker.numKeys; x++){\
                        if(marker.key(x).comment == "Win"){\
                            n = marker.key(x);\
                            if(time>n.time){\
                                result=time - n.time\
                            }\
                            else{result=0}\
                        }\
                    }\
                    result';
        },
    };
})();