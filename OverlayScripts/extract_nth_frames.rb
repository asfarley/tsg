# extract_nth_frames.rb
#
# This script finds all movie files in the subdirectory passed in as an argument. 
# Every Nth frame is exported to the location ./Frames (relative to this script) where
# the parameter N is passed as a command-line argument.
#
# Alexander Farley
# 2017-08-20
require 'fileutils'
require 'pry'

#Parse command-line arguments
if (ARGV.count < 2)
	puts "This script requires two arguments: a path to the folder containing movies, and a path to the output folder location."
	exit
end

#Get all movie files in this subdirectory
in_directory = ARGV[0]
out_directory = ARGV[1] + "/Images"

force_yes = false
if(ARGV.count == 3)
	force_yes = (ARGV[2] == "-y") or (ARGV[2] == "-Y")
end

files = Dir["#{in_directory}/**/*.*"]
accepted_formats = [".avi", ".mkv", ".mp4", ".h264", ".mov", ".wmv", ".3gp", ".asf"]
movie_files = files.select { |pathstring| accepted_formats.include? File.extname(pathstring) }

#Check if files exist in output location
if Dir.exist? out_directory	
	if force_yes
		FileUtils.remove_dir(out_directory)
	else
		puts "Output folder already exists; OK to overwrite?"
		ans = STDIN.gets
		if ans.downcase.include? "y"
			FileUtils.remove_dir(out_directory)
		else
			puts "Exiting"
			exit
		end
	end
end
Dir.mkdir out_directory

#Export the nth frame from of every movie
export_framerate = 3 # Every 3 seconds
export_movie_index = 0
movie_files.each do |movie_path|
	if force_yes
		`ffmpeg -i "#{movie_path}" -vframes 100 -r #{export_framerate} #{out_directory}/#{export_movie_index}_%03d.jpg`
	else
		puts "Export frames from #{movie_path}?"
		ans = STDIN.gets
		if ans.downcase.include? "y"
			`ffmpeg -i "#{movie_path}" -vframes 100 -r #{export_framerate} #{out_directory}/#{export_movie_index}_%03d.jpg`
		end
	end	
	export_movie_index += 1
end

#Rename files
image_files = Dir["#{out_directory}/*.*"]
jpg_accepted_formats = [".jpg"]
image_files = image_files.select { |pathstring| jpg_accepted_formats.include? File.extname(pathstring) }
image_number = 0
image_files.each do |image_file|
	image_name = "#{out_directory}/000#{image_number}.jpg"
	File.rename(image_file, image_name)
	image_number += 1
end