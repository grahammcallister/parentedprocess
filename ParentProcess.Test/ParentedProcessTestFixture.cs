using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ParentProcess.Test
{
    [TestFixture]
    public class ParentedProcessTestFixture
    {
        [TestCase]
        public void ParentProcessManagerConstructor_WithNullFilename_ThrowsArgumentNullException()
        {
            // Arrange + Act + Assert
            Assert.Throws<ArgumentNullException>(() => { new ProcessManager(null, null, null); });
        }

        [TestCase]
        public void ParentProcessManagerConstructor_WithEmptyFilename_ThrowsArgumentNullException()
        {
            // Arrange + Act + Assert
            Assert.Throws<ArgumentNullException>(() => { new ProcessManager(string.Empty, null, null); });
        }

        [TestCase]
        public void ParentProcessManagerConstructor_WithNotFoundFilename_ThrowsFileNotFoundException()
        {
            // Arrange + Act + Assert
            Assert.Throws<FileNotFoundException>(() => { new ProcessManager(@"C:\Foo\Bar.exe", null, null); });
        }

        private Exception _unhandledException;

        [TestCase]
        public void ParentProcessManager_WithFaultingApplication_FiresUnhandledExceptionEvent()
        {
            var path = System.IO.Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            var finalPath = Path.Combine(path, @"..\..\..\ParentProcess.Test.FaultingAppTest\bin\Debug\ParentProcess.Test.FaultingAppTest.exe");
            // Arrange
            var manager = new ProcessManager(finalPath, "Faulting process", "Faulting process");
            manager.ProcessUnhandledExceptionEvent += Manager_ProcessUnhandledExceptionEvent;

            // Act
            manager.StartProcess();

            Thread.Sleep(6000);

            // Assert

            Assert.That(_unhandledException, Is.Not.Null);
            Assert.That(manager.HasExited, Is.True);
        }

        private bool _wasStopped = false;

        [TestCase]
        public void ParentProcessManager_WhenStoppingProcess_FiresStoppedEventAndProcessIsNoLongerRunning()
        {
            // Arrange
            var path = @"C:\Windows\System32\Notepad.exe";
            var manager = new ProcessManager(path, "Notepad will be killed", "Notepad will be killed");
            manager.ProcessStoppedEvent += Manager_ProcessStoppedEvent;

            // Act
            manager.StartProcess();

            Thread.Sleep(1000);

            manager.StopProcess();

            Thread.Sleep(5000);

            // Assert

            Assert.That(_wasStopped, Is.True);
            Assert.That(manager.HasExited, Is.True);
            Assert.That(manager.ParentedProcessInfo.Process.HasExited, Is.True);
        }

        private void Manager_ProcessStoppedEvent(EventArgs args)
        {
            _wasStopped = true;
        }

        private void Manager_ProcessUnhandledExceptionEvent(object sender, UnhandledExceptionEventArgs args)
        {
            _unhandledException = args.ExceptionObject as Exception;
        }

        [TestCase]
        public void ParentProcessManager_StartProcessWithRunningApplication_HasExitedFalse()
        {
            // Arrange
            var path = @"C:\Windows\System32\Notepad.exe";
            var manager = new ProcessManager(path, "Notepad parented", "Notepad parented");

            // Act
            manager.StartProcess();

            Thread.Sleep(2000);

            // Assert
            Assert.That(manager, Is.Not.Null);
            Assert.That(manager.HasExited, Is.False);

            // Clean up
            manager.StopProcess();
        }

        private string _mainWindowHandle = string.Empty;
        private ProcessManager _manager;

        [TestCase]
        public void ParentProcessManager_StartProcessWithMainWindow_CanFindMainWindowHandle()
        {
            // Arrange
            var path = @"C:\Windows\System32\Notepad.exe";
            _manager = new ProcessManager(path, "Notepad parented", "Notepad parented");

            // Act
            _manager.StartProcess();
            _manager.ProcessMainWindowHandleFoundEvent += Manager_ProcessMainWindowHandleFoundEvent;
            Thread.Sleep(2000);
            _manager.FindMainWindowHandle();
            Thread.Sleep(2000);

            // Assert
            Assert.That(_manager, Is.Not.Null);
            Assert.That(_manager.HasExited, Is.False);
            Assert.That(_mainWindowHandle, Is.Not.Null);
            Assert.That(_mainWindowHandle, Is.Not.Empty);
            Assert.That(_mainWindowHandle, Is.Not.EqualTo("-1"));

            // Clean up
            _manager.ParentedProcessInfo.Process.Kill();
        }

        private void Manager_ProcessMainWindowHandleFoundEvent(EventArgs args)
        {
            _mainWindowHandle = _manager.ParentedProcessInfo.MainWindowHandle.ToString();
        }

        [TestCase]
        public void ParentProcessManager_WithApplicationKilled_FiresUnhandledExceptionEvent()
        {
            // Arrange
            var path = @"C:\Windows\System32\Notepad.exe";
            var manager = new ProcessManager(path, "Notepad", "Notepad");
            manager.ProcessUnhandledExceptionEvent += Manager_ProcessUnhandledExceptionEvent;
            manager.StartProcess();
            Thread.Sleep(3000);

            // Act
            manager.ParentedProcessInfo.Process.Kill();
            Thread.Sleep(7000);

            // Assert
            Assert.That(_unhandledException, Is.Not.Null);
            Assert.That(manager.HasExited, Is.True);
        }

        //[TestCase]
        //public async Task TimerTest()
        //{
        //    await WpfContext.Run(() =>
        //    {
        //        _value = 0;
        //        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
        //        timer.Tick += IncrementValue;
        //        timer.Start();

        //        await Task.Delay(15);
        //        Assert.AreNotEqual(0, _value);
        //    });
        //}
    }
}