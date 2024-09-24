#!/bin/bash

# Set the recording file name and aspect ratio (width:height)
OUTPUT_FILE="screen_recording.mp4"
ASPECT_RATIO="16:9"  # Change this to your desired aspect ratio
FPS=30  # Frames per second

# Get the display resolution
SCREEN_WIDTH=$(system_profiler SPDisplaysDataType | grep Resolution | awk '{print $2}')
SCREEN_HEIGHT=$(system_profiler SPDisplaysDataType | grep Resolution | awk '{print $4}')

# Calculate resolution based on the aspect ratio and the display resolution
NEW_HEIGHT=$(echo "scale=0; $SCREEN_WIDTH / (${ASPECT_RATIO%%:*} / ${ASPECT_RATIO##*:})" | bc)

if [ $NEW_HEIGHT -gt $SCREEN_HEIGHT ]; then
  NEW_WIDTH=$(echo "scale=0; $SCREEN_HEIGHT * (${ASPECT_RATIO%%:*} / ${ASPECT_RATIO##*:})" | bc)
  NEW_HEIGHT=$SCREEN_HEIGHT
else
  NEW_WIDTH=$SCREEN_WIDTH
fi

echo "Recording resolution: ${NEW_WIDTH}x${NEW_HEIGHT}"

# Start screen recording in the background
ffmpeg -f avfoundation -framerate $FPS -i "1:0" -vf "scale=${NEW_WIDTH}:${NEW_HEIGHT}" -vcodec libx264 -crf 0 -preset ultrafast $OUTPUT_FILE &

# Get the PID of the ffmpeg process
FFMPEG_PID=$!

echo "Recording started. Press Spacebar to stop."

# Get the currently focused application and press key code 49 there
osascript <<EOF
tell application "System Events"
    set frontApp to name of first application process whose frontmost is true
    tell process frontApp to key code 49
end tell
EOF

# Wait for the spacebar press to stop recording
while true; do

    if [ "$SPACEBAR_PRESSED" ]; then
        echo "Spacebar pressed, stopping recording..."
        kill $FFMPEG_PID  # Stop the ffmpeg recording
        break
    fi

    sleep 0.1  # Wait briefly before checking again
done

echo "Recording saved to $OUTPUT_FILE"