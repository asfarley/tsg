# generate_labels.rb
# Author: Alexander Farley
# Date: 2017-07-27
#
# This script converts a textfile containing bounding boxes with associated object-quantity information 
# to a label file for Theano network training.
#
# The bounding box file has the following form:
#
#Filename:"C:\VTCProject\vtc\bin\examples\MovingObjectSubimages\1.bmp" X:317.7 Y:306.2 Width:18 Height:13 Frame:207 Count:1
#Filename:"C:\VTCProject\vtc\bin\examples\MovingObjectSubimages\2.bmp" X:322.9 Y:305.9 Width:21 Height:17 Frame:217 Count:1
#Filename:"C:\VTCProject\vtc\bin\examples\MovingObjectSubimages\3.bmp" X:329.5 Y:306.5 Width:22 Height:18 Frame:227 Count:1
#Filename:"C:\VTCProject\vtc\bin\examples\MovingObjectSubimages\4.bmp" X:337.1 Y:307.7 Width:22 Height:19 Frame:237 Count:1
#Filename:"C:\VTCProject\vtc\bin\examples\MovingObjectSubimages\5.bmp" X:346.4 Y:308.7 Width:24 Height:19 Frame:248 Count:1 
#
#
# The label file has the following form:
#
#Classes:  car  bus  truck  person  motorcycle  none
#"7.bmp" car [1 0 0 0 0 0]
#"8.bmp" car [1 0 0 0 0 0]
#"9.bmp" car [1 0 0 0 0 0]
#"32.bmp" car [1 0 0 0 0 0]
#"33.bmp" car [1 0 0 0 0 0]
#"52.bmp" car [1 0 0 0 0 0]
#
#
# To use this script, supply it with arguments for the input and output filepaths.
# ruby generate_labels.rb boundingboxes.txt labels.txt

require 'pry'

if ARGV.count != 2
	puts "This script requires 2 arguments (input file path and output file path), #{ARGV.count} arguments received"
	exit
end

bounding_box_path = ARGV[0]
labels_path = ARGV[1]

if File.file? labels_path
	puts "Output file already exists; delete?"
	ans = STDIN.gets
	puts "Received #{ans}"
	if ans.downcase.include? "y" 
		puts "Ok, overwriting."
		File.delete labels_path
	else
		puts "Output not written, exiting."
		exit
	end
end

# Do a first pass through the object-quantity file to determine the maximum number of object present in any particular subimage.
# This is necessary in order to write the header for the output file, which describes the output vector for the training set.
max_count = 0
puts "Seeking for max object count..."
File.readlines(bounding_box_path).each do |line|
	split_line = line.split(" ")
	path_segment = split_line[0]
	count_segment = split_line[6]
	path = path_segment[9..-1]
	count = count_segment[6..-1].to_i
	max_count = (count > max_count) ? count : max_count
end
puts "Max object count is #{max_count}"


# open and write to a file with ruby
open(labels_path, 'w') { |f|
	possible_values = (0..max_count).to_a
	possible_values_string = possible_values.join(" ")
	f.puts "Classes: #{possible_values_string}"
	puts "Writing converted training examples..."
	File.readlines(bounding_box_path).each do |line|
		split_line = line.split(" ")
		path_segment = split_line[0]
		count_segment = split_line[6]
		path = path_segment[9..-1]
		count = count_segment[6..-1].to_i
		output_vector = Array.new(max_count+1,0)
		output_vector[count] = 1
		output_vector_string = output_vector.join(" ")
		f.puts "#{path} #{count} [#{output_vector_string}]"
	end
}
puts "Done."