require 'oily_png'
require 'optparse'
require './TrajectoryBundle'

#Get image files
options = {}
OptionParser.new do |opts|
	opts.banner = "Usage: overlay.rb [options]"
	opts.on('-i', '--imagepath IMAGE', 'images path') { |v| options[:image] = v }
	opts.on('-s', '--statepath STATE', 'states path') { |v| options[:state] = v }
	opts.on('-o', '--outpath OUTPUT', 'output path') { |v| options[:output] = v }
end.parse!

#Validate images path
pngFilesWildcardPath = File.join( options[:image], "*.png").gsub('\\', '/')
imageCount = Dir[pngFilesWildcardPath].count
if(imageCount < 1)
	puts "No images found in #{pngFilesWildcardPath}, exiting."
	exit
else
	puts "Discovered #{imageCount} images"
end

#Validate states path
stateCount = TrajectoryBundle.getStateHistoryCount(options[:state])
if(stateCount < 1)
	puts "No state logs found in #{options[:state]}, exiting."
	exit
else
	puts "Discovered #{stateCount} state logs"
end

#Validate output path
if not File.directory?(options[:output])
	puts "Output path does not exist, exiting."
	exit
end

#Read in state logs
bundle = TrajectoryBundle.new
bundle.readFromFile(options[:state])

#Find all .png files in the images path
nFiles = Dir[pngFilesWildcardPath].length
nProcessedFiles = 0
Dir[pngFilesWildcardPath].each do |file|
	#If overlay exists for this frame
	filename = File.basename file
	frame = filename[0..-3].to_i

	percent = (100.0*(nProcessedFiles/nFiles)).round()
	puts "Processing: #{percent}% complete (frame #{nProcessedFiles}"
	
	overlayObjects = bundle.objectsInFrame(frame)
	image = ChunkyPNG::Image.from_file(file)
	overlayObjects.each do  |object|
		x0 = (object.x - object.width/2).round
		y0 = (object.y - object.height/2).round
		x1 = (object.x + object.width/2).round
		y1 = (object.y + object.height/2).round
		#puts "Rect X0:#{x0} Y0:#{y0} X1:#{x1} Y1:#{y1}"
		image = image.rect(x0, y0, x1, y1, stroke_color = ChunkyPNG::Color::WHITE)
	end
	
	basename = File.basename file
	filepath = File.join(options[:output], basename)
	image.save(filepath)
	nProcessedFiles = nProcessedFiles+1
end
