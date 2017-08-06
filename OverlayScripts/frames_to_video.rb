# frames_to_video.rb
# 
# Author: Alexander Farley
# Date: 2017-07-30
#
# This script accepts a path to a folder containing sequential video frames and
# converts the images to a video file.
#
# Use the script like this:
# ruby frames_to_video.rb ./inputframes ./inputframes/output.avi

if(ARGV.count != 2)
	puts "This script require 2 arguments: input folder and output video path."
end

input_frames_path = ARGV[0]
output_video_path = ARGV[1]

`ffmpeg -s 1920x1080 -i #{input_frames_path}%08d.png -vcodec libx264 -b 4M -crf 15 #{output_video_path}`